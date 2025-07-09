using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager.Connection
{
    /// <summary>
    /// Manages the auto-reconnection logic for a manager connection.
    /// It listens to the lifecycle manager's events and triggers reconnection attempts
    /// based on the configured parameters.
    /// </summary>
    public class ConnectionReconnector : IDisposable
    {
        private readonly ILogger _logger = ManagerLogger.CreateLogger<ConnectionReconnector>();
        private readonly ReconnectorParameters _parameters;
        private readonly IAMISocketManager _lifecycleManager;
        private readonly ConnectionAuthenticator _authenticator;
        private readonly ConnectionLivenessMonitor _livenessMonitor;

        private readonly object _reconnectLock = new object();
        private bool _isReconnecting = false;

        /// <summary>
        /// Tracks the number of disconnections that have occurred.
        /// </summary>
        /// <remarks>This field is used internally to maintain a count of disconnection events. It is not
        /// intended for direct access or modification by external code.</remarks>
        private long _disconnectionCounter = 0;

        private bool _disposed = false;

        public ConnectionReconnector(
            ReconnectorParameters parameters,
            IAMISocketManager lifecycleManager,
            ConnectionAuthenticator authenticator,
            ConnectionLivenessMonitor livenessMonitor)
        {
            _parameters = parameters;
            _lifecycleManager = lifecycleManager;
            _authenticator = authenticator;
            _livenessMonitor = livenessMonitor;
        }

        /// <summary>
        /// Starts listening for disconnection events to handle auto-reconnect.
        /// </summary>
        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ConnectionReconnector));

            _lifecycleManager.OnDisconnected += HandleDisconnected;
            _logger.LogInformation("ConnectionReconnector started and subscribed to OnDisconnected event. KeepAlive: {KeepAlive}, ReconnectRetryMax: {ReconnectRetryMax}", 
                _parameters.KeepAlive, _parameters.ReconnectRetryMax);
        }

        /// <summary>
        /// Stops listening to events to prevent memory leaks.
        /// </summary>
        public void Stop()
        {
            if (_disposed)
                return; // Silently return if already disposed

            _lifecycleManager.OnDisconnected -= HandleDisconnected;
            _logger.LogDebug("ConnectionReconnector stopped.");
        }

        private void HandleDisconnected(object? sender, DisconnectEventArgs e)
        {
            if (_disposed)
            {
                _logger.LogDebug("HandleDisconnected called on disposed ConnectionReconnector, ignoring");
                return;
            }

            _logger.LogWarning("ConnectionReconnector.HandleDisconnected called - sender: {Sender}, cause: {Cause}, permanent: {IsPermanent}", 
                sender?.GetType().Name ?? "null", e.Cause, e.IsPermanent);
            
            var totalFailures = Interlocked.Increment(ref _disconnectionCounter);
            _logger.LogWarning("Connection lost! Failure count for this instance: {TotalFailures}. Cause: {Cause}. Permanent: {IsPermanent}", totalFailures, e.Cause, e.IsPermanent);

            // É importante parar o monitor de liveness assim que a desconexão ocorre.
            _livenessMonitor.Stop();

            // Só tenta reconectar se a opção KeepAlive estiver ativa e
            // a desconexão não for intencional (ex: LogOff ou Dispose).
            if (!_parameters.KeepAlive || e.IsPermanent)
            {
                _logger.LogInformation("Reconnection will not be attempted. (KeepAlive: {KeepAlive}, IsPermanent: {IsPermanent})", _parameters.KeepAlive, e.IsPermanent);
                return;
            }

            _logger.LogInformation("Reconnection will be attempted. KeepAlive: {KeepAlive}, IsPermanent: {IsPermanent}", _parameters.KeepAlive, e.IsPermanent);

            lock (_reconnectLock)
            {
                if (_disposed)
                {
                    _logger.LogDebug("ConnectionReconnector disposed during reconnection attempt, aborting");
                    return;
                }

                if (_isReconnecting)
                {
                    _logger.LogInformation("Reconnection attempt is already in progress.");
                    return;
                }
                _isReconnecting = true;
                _logger.LogInformation("Starting new reconnection task...");
            }

            // Inicia a tentativa de reconexão em uma nova tarefa para não bloquear o chamador do evento
            _logger.LogInformation("About to start TryReconnectAsync...");
            _ = TryReconnectAsync();
        }

        private async Task TryReconnectAsync()
        {
            try
            {
                // Check if disposed before starting
                if (_disposed)
                {
                    _logger.LogDebug("TryReconnectAsync aborted: ConnectionReconnector is disposed");
                    return;
                }

                // NEW: Check if we should retry indefinitely
                if (_parameters.ReconnectRetryMax == 0)
                {
                    // Handle the infinite retry case
                    int attempt = 1;
                    while (!_disposed) // Check disposal in loop condition
                    {
                        // Determine the delay interval based on the attempt number
                        int delayMs = (attempt <= _parameters.ReconnectRetryFast)
                            ? _parameters.ReconnectIntervalFast
                            : _parameters.ReconnectIntervalMax;

                        _logger.LogInformation("Reconnect attempt {Attempt} (infinite retries). Waiting for {Delay}ms.", attempt, delayMs);

                        await Task.Delay(delayMs);

                        // Check if disposed after delay
                        if (_disposed)
                        {
                            _logger.LogDebug("TryReconnectAsync aborted during delay: ConnectionReconnector is disposed");
                            return;
                        }

                        try
                        {
                            _logger.LogInformation("Attempting to log in (Attempt {Attempt})...", attempt);
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                            // Use the injected authenticator to attempt login
                            await _authenticator.Login(cts.Token);

                            _logger.LogInformation("Reconnect successful!");

                            // restart the liveness monitor for the new connection
                            _livenessMonitor.Start();
                            return; // Success! Exit the method.
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Reconnect attempt {Attempt} failed.", attempt);
                        }

                        attempt++;
                    }
                }
                else
                {
                    // Handle the finite retry case (original logic)
                    long totalMaxRetriesLong = (long)_parameters.ReconnectRetryFast + _parameters.ReconnectRetryMax;
                    if (totalMaxRetriesLong <= 0)
                    {
                        _logger.LogError("Invalid ReconnectRetry values. Stopping reconnection.");
                        return;
                    }

                    int totalMaxRetries = (int)Math.Min(totalMaxRetriesLong, int.MaxValue);

                    for (int attempt = 1; attempt <= totalMaxRetries && !_disposed; attempt++)
                    {
                        int delayMs = (attempt <= _parameters.ReconnectRetryFast) ? _parameters.ReconnectIntervalFast : _parameters.ReconnectIntervalMax;
                        _logger.LogInformation("Reconnect attempt {Attempt}/{MaxAttempts}. Waiting for {Delay}ms.", attempt, totalMaxRetries, delayMs);

                        await Task.Delay(delayMs);

                        // Check if disposed after delay
                        if (_disposed)
                        {
                            _logger.LogDebug("TryReconnectAsync aborted during finite retry: ConnectionReconnector is disposed");
                            return;
                        }

                        try
                        {
                            _logger.LogInformation("Attempting to log in (Attempt {Attempt})...", attempt);
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                            await _authenticator.Login(cts.Token);

                            _logger.LogInformation("Reconnect successful!");
                            _livenessMonitor.Start();
                            return; // Success!
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Reconnect attempt {Attempt} failed.", attempt);
                        }
                    }
                    _logger.LogError("All {MaxAttempts} reconnect attempts failed. Stopping reconnection attempts.", totalMaxRetries);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred within the TryReconnectAsync method.");
            }
            finally
            {
                lock (_reconnectLock)
                {
                    _isReconnecting = false;
                }
            }
        }
        /// <summary>
        /// Stops listening to events to prevent memory leaks.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected virtual dispose method for proper disposal pattern implementation.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources, false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _logger.LogDebug("Disposing ConnectionReconnector managed resources");
                    Stop();
                }

                // Dispose unmanaged resources (none in this class currently)
                
                _disposed = true;
            }
        }
    }
}