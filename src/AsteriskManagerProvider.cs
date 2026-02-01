using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk.Manager.Connection;
using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager
{   
    /// <summary>
    /// AsteriskManagerProvider is designed for short-term, single-action connections to Asterisk servers.
    /// This provider is optimized for quick operations where you connect, execute actions, and disconnect.
    /// </summary>
    /// <remarks>
    /// AsteriskManagerProvider Purpose:
    /// - Quick, temporary connections for specific operations
    /// - Execute one or more actions and then disconnect
    /// - Manual connection lifecycle management
    /// - Suitable for scripts, automation, and one-time operations
    /// - Minimal resource usage for short-lived tasks
    /// 
    /// Use AsteriskManagerProvider when you need:
    /// - Execute specific AMI actions (Originate, Hangup, Command, etc.)
    /// - One-time or periodic operations (scripts, scheduled tasks)
    /// - Manual control over connection lifecycle
    /// - Minimal overhead for simple operations
    /// - No need for event monitoring
    /// 
    /// Do NOT use AsteriskManagerProvider for:
    /// - Long-running background services (use AMIService instead)
    /// - Continuous event monitoring
    /// - Real-time telephony state tracking
    /// - Applications that need persistent connections
    /// 
    /// Usage Pattern:
    /// ```csharp
    /// // Connect, execute action, disconnect
    /// using var provider = new AsteriskManagerProvider(options, logger);
    /// using var connection = await provider.ConnectAsync(keepAlive: false);
    /// var response = await connection.SendActionAsync(myAction);
    /// // Automatically disconnects when disposed
    /// ```
    /// 
    /// Configuration Notes:
    /// - Set keepAlive=false for temporary connections
    /// - Set keepAlive=true only if you need the connection to persist beyond the immediate operation
    /// - Uses the same AMIProviderOptions as AMIService but with different usage patterns
    /// </remarks>
    public class AsteriskManagerProvider : IAsteriskManagerProvider
    {
        public AMIProviderOptions Options { get; internal set; }

        public string Title => Options.Title;

        public bool Enabled { get; private set; }

        [JsonIgnore]
        public IManagerConnection? Connection => _connection;

        private ManagerConnection? _connection;
        private readonly ILogger _logger;
        private readonly object _lockConnection = new object();

        private readonly SemaphoreSlim _stateChangeSemaphore = new(1, 1);

        public AsteriskManagerProvider(IOptions<AMIProviderOptions> options, ILogger<AsteriskManagerProvider> logger)
        {
            _logger = logger;
            Options = options.Value;
        }

        public Task<IManagerConnection> ConnectAsync(CancellationToken cancellationToken = default)
            => ConnectAsync(null, cancellationToken);

        /// <summary>
        /// Asynchronously connects the provider to the Asterisk server and returns the valid connection object.
        /// </summary>
        public async Task<IManagerConnection> ConnectAsync(bool? keepalive = null, CancellationToken cancellationToken = default)
        {
            await _stateChangeSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (Enabled && _connection != null && _connection.IsConnected)
                {
                    _logger.LogInformation("provider '{Title}' is already connected.", Options.Title);
                    return _connection;
                }

                _logger.LogDebug("Asterisk Manager Provider: '{Title}', connecting ...", Options.Title);
                var connection = await InternalConnect(keepalive ?? Options.KeepAlive, cancellationToken);
                Enabled = true;
                _logger.LogInformation("Asterisk Manager Provider: '{Title}', connected successfully, with username: {username}.", Options.Title, Options.Username);

                return connection;
            }
            finally
            {
                _stateChangeSemaphore.Release();
            }
        }

        /// <summary>
        /// Asynchronously disconnects the provider from the Asterisk server.
        /// </summary>
        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            await _stateChangeSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (!Enabled)
                {
                    _logger.LogInformation("provider '{Title}' is already disconnected.", Options.Title);
                    return;
                }

                _logger.LogInformation("disconnecting Asterisk Manager Provider: {Title}", Options.Title);
                await InternalDisconnect(cancellationToken);
                Enabled = false;
                _logger.LogInformation("provider '{Title}' disconnected successfully.", Options.Title);
            }
            finally
            {
                _stateChangeSemaphore.Release();
            }
        }

        public ValueTask<IManagerConnection> GetValidConnection(CancellationToken cancellationToken = default)
            => InternalConnect(false, cancellationToken);

        private async ValueTask<IManagerConnection> InternalConnect(bool keepAlive = true, CancellationToken cancellationToken = default)
        {
            lock (_lockConnection)
            {
                if (_connection == null || _connection.IsDisposed)
                {
                    Options.KeepAlive = keepAlive;
                    Options.ReconnectIntervalMax = 30000;

                    _connection = new AMIConnection(Options);
                    
                    // Subscribe to connection events for diagnostic logging
                    _connection.OnDisconnected += HandleConnectionDisconnected;
                }
            }

            if (!_connection.IsConnected)
            {
                try
                {
                    await _connection.Login(cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log authentication/connection failures with server details
                    _logger.LogError(ex, "AMI Connection failed! Host: {Host}:{Port}, User: {Username}",
                        Options.Address, Options.Port, Options.Username);
                    throw; // Re-throw to maintain original behavior
                }
            }

            return _connection;
        }
        
        /// <summary>
        /// Handles connection disconnect events and logs diagnostic information
        /// </summary>
        private void HandleConnectionDisconnected(object? sender, Connection.DisconnectEventArgs e)
        {
            var address = Options.Address;
            var port = Options.Port;
            var username = Options.Username;
            
            _logger.LogError("AMI Connection lost! Host: {Host}:{Port}, User: {Username}, Cause: {Cause}, Permanent: {IsPermanent}",
                address, port, username, e.Cause, e.IsPermanent);
        }

        private Task InternalDisconnect(CancellationToken cancellationToken = default)
        {
            if (_connection?.IsConnected ?? false)
            {
                return _connection.LogOff(cancellationToken);
            }
            return Task.CompletedTask;
        }

        #region DISPOSING

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Synchronously disposes of the object. Use DisposeAsync() for a graceful shutdown.
        /// </summary>
        public void Dispose()
        {
            // Call the full dispose method, assuming this is a direct call from user code.
            Dispose(true);

            // Suppress finalization to prevent the finalizer from running.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            // Mark as disposed to prevent multiple calls.
            IsDisposed = true;

            // Only dispose managed resources if we are called from Dispose() or DisposeAsync().
            if (disposing)
            {
                _logger.LogInformation("Synchronous disposal initiated for provider '{Title}'.", Options.Title);

                // We're in the synchronous path, so we don't await.
                // We attempt to get the semaphore without blocking indefinitely.
                if (_stateChangeSemaphore.Wait(TimeSpan.FromSeconds(5)))
                {
                    try
                    {
                        // Clean up the connection if it exists.
                        if (_connection != null)
                        {
                            // Unsubscribe from events
                            _connection.OnDisconnected -= HandleConnectionDisconnected;
                            
                            // Attempt to log off, but don't block. Use a timeout.
                            // If the task doesn't complete in time, we just continue.
                            if (_connection.IsConnected)
                            {
                                try
                                {
                                    // Use a non-blocking wait. This is better than a hard Wait().
                                    // It allows the thread to continue after the timeout.
                                    var disconnectTask = _connection.LogOff(CancellationToken.None);
                                    if (!disconnectTask.Wait(TimeSpan.FromSeconds(2)))
                                    {
                                        _logger.LogWarning("Timeout while waiting for connection logoff during synchronous dispose.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error during connection logoff in synchronous dispose.");
                                    // Ignoring errors as per original code logic on dispose.
                                }
                            }
                            _connection.Dispose();
                            _connection = null;
                        }
                    }
                    finally
                    {
                        // Always release the semaphore.
                        _stateChangeSemaphore.Release();
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to acquire semaphore during synchronous dispose. Connection might not be properly cleaned up.");
                }

                // The semaphore itself needs to be disposed.
                _stateChangeSemaphore.Dispose();
            }
        }

        /// <summary>
        /// Asynchronously disposes of the object, ensuring a graceful shutdown.
        /// This is the recommended method for disposing of the provider.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (IsDisposed)
            {
                return;
            }

            _logger.LogInformation("Asynchronous disposal initiated for provider '{Title}'.", Options.Title);

            // Wait for the semaphore without a timeout, ensuring we have exclusive access.
            await _stateChangeSemaphore.WaitAsync();
            try
            {
                // Disconnect gracefully. The method already uses an awaitable task.
                if (_connection?.IsConnected ?? false)
                {
                    await _connection.LogOff(CancellationToken.None);
                }

                // Dispose the connection.
                if (_connection != null)
                {
                    // Unsubscribe from events
                    _connection.OnDisconnected -= HandleConnectionDisconnected;
                    
                    _connection.Dispose();
                    _connection = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during asynchronous disposal.");
                // This catch block is important to ensure the finally block is reached.
            }
            finally
            {
                // Always release the semaphore.
                _stateChangeSemaphore.Release();
                // We're done with the semaphore, so dispose it here.
                _stateChangeSemaphore.Dispose();
            }

            // Call the synchronous dispose method to perform the final cleanup of managed resources.
            // This also handles marking the object as disposed and suppressing finalization.
            // We pass 'false' because we've already done the cleanup in the async method.
            Dispose(disposing: false);

            // Mark the object as disposed and suppress finalization.
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}