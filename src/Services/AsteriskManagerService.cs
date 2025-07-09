using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk.Manager.Connection;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sufficit.Asterisk.Manager.Services
{
    /// <summary>
    /// Abstract base class for multi-provider AMI services.
    /// Provides core functionality for managing multiple Asterisk connections
    /// with automatic reconnection and health monitoring.
    /// </summary>
    /// <remarks>
    /// This base class handles:
    /// - Multiple provider management and lifecycle
    /// - Automatic connection establishment with configurable retry logic
    /// - Connection state management and cleanup
    /// - Built-in health monitoring for all providers
    /// - Basic service infrastructure
    /// - ASP.NET Core integration (IHealthCheck, IHostedService)
    /// 
    /// Derived classes should implement:
    /// - Event handler setup (GetEventHandler)
    /// - Business-specific logic and configurations
    /// - Custom command implementations
    /// - Service lifecycle management (if using as BackgroundService)
    /// - Extended health checks (via OnGetExtendedHealthData method)
    /// </remarks>
    public abstract class AsteriskManagerService : IDisposable, IAsyncDisposable, IAsteriskManagerService
    {
        protected readonly ILoggerFactory _loggerFactory;
        protected readonly ILogger _logger;
        private CancellationTokenSource? _cts;

        /// <summary>
        /// Health checker instance for performing health assessments.
        /// </summary>
        private readonly AsteriskManagerHealthChecker _healthChecker;

        /// <summary>
        /// Collection of AMI providers managed by this service.
        /// </summary>
        public ICollection<AsteriskManagerProvider> Providers { get; protected set; }

        /// <summary>
        /// Event handler for Asterisk manager events.
        /// Must be implemented by derived classes.
        /// </summary>
        public abstract IAsteriskEventManager Events { get; }

        /// <summary>
        /// Last time an event was received from any provider.
        /// Used for health monitoring.
        /// </summary>
        public DateTimeOffset LastReceivedEvent { get; protected set; }

        /// <summary>
        /// Event fired when a manager event is received from any connected provider.
        /// Provides business-specific event handling for AMI events.
        /// </summary>
        /// <remarks>
        /// This event is typically used for:
        /// - SignalR real-time notifications
        /// - Business logic triggered by telephony events
        /// - Integration with external systems
        /// - Call monitoring and analytics
        /// </remarks>
        public virtual event EventHandler<IManagerEvent>? OnManagerEvent;

        protected AsteriskManagerService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger(this.GetType());
            _logger.LogDebug("AsteriskManagerService initialized");

            Providers = new HashSet<AsteriskManagerProvider>();
            
            // Initialize health checker
            var healthLogger = _loggerFactory.CreateLogger<AsteriskManagerHealthChecker>();
            _healthChecker = new AsteriskManagerHealthChecker(healthLogger);
        }

        /// <summary>
        /// Abstract method to get the event handler configuration.
        /// Derived classes must implement this to define how events are handled.
        /// </summary>
        /// <returns>Configured AsteriskEventManager instance</returns>
        protected abstract IAsteriskEventManager GetEventHandler();

        /// <summary>
        /// Abstract method to get provider configurations.
        /// Derived classes must implement this to provide their provider configuration.
        /// </summary>
        /// <returns>Collection of provider options</returns>
        protected abstract IEnumerable<AMIProviderOptions> GetProviderConfigurations();

        /// <summary>
        /// Gets the service name for logging and identification.
        /// Can be overridden by derived classes.
        /// </summary>
        protected virtual string ServiceName => nameof(AsteriskManagerService);
        
        #region Service Management

        /// <summary>
        /// Starts the service with the given cancellation token.
        /// </summary>
        public virtual Task StartAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            _logger.LogInformation("{ServiceName} starting...", ServiceName);

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var configurations = GetProviderConfigurations();
            if (!configurations.Any())
                throw new InvalidOperationException("No provider configurations found");

            Providers = LoadProviders(configurations);

            // Start connection attempts for each provider with endless retry
            foreach (var provider in Providers)
            {
                // Start a background task for each provider to handle endless connection attempts
                _ = Task.Run(async () => await ConnectProviderWithEndlessRetry(provider, _cts.Token), _cts.Token);
                _logger.LogInformation("Provider {Title} connection task started", provider.Title);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        public virtual Task StopAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            _logger.LogInformation("{ServiceName} stopping...", ServiceName);
            
            _cts?.Cancel();
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Runs the service until cancellation is requested.
        /// This method can be used by derived classes that implement BackgroundService.
        /// </summary>
        public async Task RunUntilCancellationAsync(CancellationToken cancellationToken)
        {
            if (_cts != null)
            {
                // awaiting infinite until cancellation triggered
                await Task.Delay(Timeout.Infinite, _cts.Token);
            }
        }

        /// <summary>
        /// Attempts to connect a provider with configurable retry logic for initial connections.
        /// Uses the provider's InitialRetry configuration to determine retry behavior.
        /// Only stops for authentication failures (if configured) or cancellation requests.
        /// </summary>
        private async Task ConnectProviderWithEndlessRetry(AsteriskManagerProvider provider, CancellationToken cancellationToken)
        {
            var retryConfig = provider.Options.InitialRetry;

            // If initial retry is disabled, try only once
            if (!retryConfig.EnableInitialRetry)
            {
                try
                {
                    _logger.LogDebug("Attempting single connection for provider {Title} (retry disabled)", provider.Title);

                    // For persistent services: Always use keepAlive=true
                    var connection = await provider.ConnectAsync(true, cancellationToken);
                    connection.Use(Events);
                    _logger.LogInformation("Provider {Title} connected successfully!", provider.Title);

                    await ExecuteProviderMonitoringAsync(connection, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect provider {Title} - retry disabled", provider.Title);
                }
                return;
            }

            int attempt = 1;
            int currentDelayMs = retryConfig.InitialRetryDelayMs;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("Attempting to connect provider {Title}, attempt {Attempt}", provider.Title, attempt);

                    // For persistent services: Always use keepAlive=true for persistent event monitoring
                    var connection = await provider.ConnectAsync(true, cancellationToken);

                    // Connection successful - setup event listener for persistent monitoring
                    // IMPORTANT: Use the same event manager instance to preserve subscriptions
                    // Don't dispose the old event manager as it should be shared across connections
                    connection.Use(Events, disposable: false);
                    _logger.LogInformation("Provider {Title} connected successfully on attempt {Attempt}!", provider.Title, attempt);

                    // Reset delay for next potential connection cycle
                    currentDelayMs = retryConfig.InitialRetryDelayMs;

                    // Execute the main monitoring loop for this connection
                    await ExecuteProviderMonitoringAsync(connection, cancellationToken);

                    // If we get here, connection was lost - reset attempt counter for logging
                    attempt = 1;
                    _logger.LogWarning("Provider {Title} connection lost, will retry...", provider.Title);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Connection attempts for provider {Title} cancelled", provider.Title);
                    break;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("not authenticated") && retryConfig.StopOnAuthenticationFailure)
                {
                    // Authentication failure - stop if configured to do so
                    _logger.LogError(ex, "Authentication failure for provider {Title} - stopping retry attempts", provider.Title);
                    break;
                }
                catch (Exception ex)
                {
                    // Network or other temporary errors - retry with progressive delay
                    _logger.LogWarning(ex, "Failed to connect provider {Title} on attempt {Attempt}, retrying in {Delay}ms",
                        provider.Title, attempt, currentDelayMs);

                    try
                    {
                        await Task.Delay(currentDelayMs, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Retry delay cancelled for provider {Title}", provider.Title);
                        break;
                    }

                    // Check if we've reached max attempts (0 means unlimited)
                    if (retryConfig.MaxInitialRetryAttempts > 0 && attempt >= retryConfig.MaxInitialRetryAttempts)
                    {
                        _logger.LogError("Provider {Title} reached maximum retry attempts ({Max}), stopping",
                            provider.Title, retryConfig.MaxInitialRetryAttempts);
                        break;
                    }

                    attempt++;

                    // Increase delay progressively, up to the maximum
                    currentDelayMs = Math.Min(
                        currentDelayMs + retryConfig.DelayIncrementMs,
                        retryConfig.MaxRetryDelayMs
                    );
                }
            }
        }

        /// <summary>
        /// Executes the main monitoring loop for a connected provider.
        /// This method represents the core execution logic following BackgroundService patterns.
        /// Monitors the connection state and handles disconnection events gracefully.
        /// </summary>
        /// <param name="connection">The established manager connection to monitor</param>
        /// <param name="cancellationToken">Cancellation token to stop monitoring</param>
        /// <returns>A task that completes when the connection is lost or cancellation is requested</returns>
        /// <remarks>
        /// This method follows the ExecuteAsync pattern commonly used in BackgroundService implementations.
        /// It provides a clean separation between connection establishment logic and monitoring logic.
        /// </remarks>
        private async Task ExecuteProviderMonitoringAsync(IManagerConnection connection, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();

            // Subscribe to disconnection events
            void OnDisconnected(object? sender, DisconnectEventArgs args)
            {
                connection.OnDisconnected -= OnDisconnected;
                tcs.TrySetResult(true);
            }

            connection.OnDisconnected += OnDisconnected;

            try
            {
                // Wait for either disconnection or cancellation
                var disconnectionTask = tcs.Task;
                var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);

                await Task.WhenAny(disconnectionTask, cancellationTask);
            }
            finally
            {
                // Cleanup
                connection.OnDisconnected -= OnDisconnected;
                tcs.TrySetCanceled(cancellationToken);
            }
        }

        #endregion

        #region Provider Management

        /// <summary>
        /// Loads providers from configuration options.
        /// Reuses existing providers when possible and disposes unused ones.
        /// </summary>
        protected virtual HashSet<AsteriskManagerProvider> LoadProviders(IEnumerable<AMIProviderOptions> options)
        {
            var providers = new HashSet<AsteriskManagerProvider>();
            foreach (var opt in options)
            {
                var provider = Providers.FirstOrDefault(s => s.Options.Equals(opt));
                if (provider == null)
                {
                    // instantiate new provider
                    provider = CreateProvider(opt);
                }

                providers.Add(provider);
            }

            // dispose instances that are not used anymore
            foreach (var provider in Providers)
            {
                if (!providers.Any(s => s.Options.Equals(provider.Options)))
                    provider.Dispose();
            }

            return providers;
        }

        /// <summary>
        /// Creates a new provider instance with proper logging.
        /// Can be overridden by derived classes for custom provider creation.
        /// </summary>
        protected virtual AsteriskManagerProvider CreateProvider(AMIProviderOptions options)
        {
            var logger = _loggerFactory.CreateLogger<AsteriskManagerProvider>();
            _logger.LogDebug("Creating new provider: {Title}", options.Title);

            return new AsteriskManagerProvider(Options.Create(options), logger);
        }

        #endregion

        #region Health Check Support

        /// <summary>
        /// Performs a comprehensive health check of all providers and the service state.
        /// This method can be easily converted to ASP.NET Core IHealthCheck format.
        /// </summary>
        /// <returns>Detailed health check result</returns>
        public virtual AsteriskManagerHealthCheckResult CheckHealthDetailed()
        {
            return _healthChecker.CheckHealth(Providers, LastReceivedEvent, OnGetExtendedHealthData);
        }

        /// <summary>
        /// Simple health check that returns the basic status for backward compatibility.
        /// </summary>
        /// <returns>Tuple with health status and message</returns>
        public virtual (bool IsHealthy, string Status) CheckHealth()
        {
            var detailed = CheckHealthDetailed();
            return detailed.ToSimpleResult();
        }

        /// <summary>
        /// Virtual method that derived classes can override to provide extended health data.
        /// This is called during health checks to include business-specific information.
        /// </summary>
        /// <returns>Dictionary of extended health data</returns>
        /// <remarks>
        /// Override this method to add:
        /// - Business-specific health indicators
        /// - Custom metrics and counters
        /// - Integration health checks (databases, external services)
        /// - Application-specific status information
        /// 
        /// Example override:
        /// <code>
        /// protected override Dictionary&lt;string, object&gt; OnGetExtendedHealthData()
        /// {
        ///     return new Dictionary&lt;string, object&gt;
        ///     {
        ///         ["ActiveCalls"] = GetActiveCallCount(),
        ///         ["QueueStatus"] = GetQueueHealthStatus(),
        ///         ["DatabaseConnected"] = IsDatabaseConnected()
        ///     };
        /// }
        /// </code>
        /// </remarks>
        protected virtual Dictionary<string, object>? OnGetExtendedHealthData()
        {
            // Override in derived classes for extended health data
            // Return null by default to indicate no extended data
            return null;
        }

        /// <summary>
        /// Asynchronous version of health check for integration with ASP.NET Core IHealthCheck.
        /// Currently executes synchronously, but allows for future asynchronous health checks.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Detailed health check result</returns>
        public virtual Task<AsteriskManagerHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            // Health check is currently synchronous, but this method allows for
            // future asynchronous health checks (e.g., database connections, external APIs)
            return Task.FromResult(CheckHealthDetailed());
        }

        /// <summary>
        /// ASP.NET Core IHealthCheck implementation.
        /// Converts internal health check to ASP.NET Core format.
        /// </summary>
        /// <param name="context">Health check context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>ASP.NET Core HealthCheckResult</returns>
        async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> IHealthCheck.CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken)
        {
            var detailed = await CheckHealthAsync(cancellationToken);
            return _healthChecker.ToAspNetHealthCheckResult(detailed);
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Base event handler that updates the last received event timestamp.
        /// Can be extended by derived classes for additional processing.
        /// </summary>
        protected virtual void HandleManagerEvent(object? sender, IManagerEvent managerEvent)
        {
            LastReceivedEvent = DateTimeOffset.UtcNow;

            string title = string.Empty;
            if (sender is AMIConnection managerConnection)
                title = managerConnection.Title;

            // Derived classes can override this for additional processing
            OnManagerEventReceived(title, managerEvent);
        }

        /// <summary>
        /// Template method for derived classes to handle manager events.
        /// </summary>
        /// <param name="senderTitle">Title of the provider that sent the event</param>
        /// <param name="managerEvent">The manager event</param>
        protected virtual void OnManagerEventReceived(string senderTitle, IManagerEvent managerEvent)
        {
            // Fire the public event for business-specific event handling
            OnManagerEvent?.Invoke(senderTitle, managerEvent);
        }

        #endregion

        #region Service Management

        /// <summary>
        /// Reloads the service with updated configuration.
        /// Can be overridden by derived classes for custom reload logic.
        /// </summary>
        public virtual async Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            await StopAsync(cancellationToken);
            await Task.Delay(500, cancellationToken); // Brief pause
            await StartAsync(cancellationToken);
        }

        #endregion

        #region Disposal

        /// <summary>
        /// Flag to detect redundant calls to Dispose
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Asynchronous disposal method. Preferred over synchronous Dispose when possible.
        /// </summary>
        /// <returns>A ValueTask representing the asynchronous disposal operation</returns>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Core asynchronous disposal logic.
        /// </summary>
        /// <returns>A ValueTask representing the asynchronous disposal operation</returns>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_disposed)
                return;

            _logger.LogInformation("{ServiceName} starting asynchronous disposal...", ServiceName);

            // Cancel any ongoing operations
            try
            {
                _cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // CancellationTokenSource was already disposed, ignore
            }

            // Dispose all providers asynchronously if they support it
            if (Providers != null)
            {
                var disposalTasks = new List<Task>();

                foreach (var provider in Providers)
                {
                    if (provider != null)
                    {
                        try
                        {
                            // If provider implements IAsyncDisposable, use async disposal
                            if (provider is IAsyncDisposable asyncDisposableProvider)
                            {
                                disposalTasks.Add(asyncDisposableProvider.DisposeAsync().AsTask());
                            }
                            else
                            {
                                // Fallback to synchronous disposal in a task
                                disposalTasks.Add(Task.Run(() => provider.Dispose()));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error disposing provider {Title}", provider.Title);
                        }
                    }
                }

                // Wait for all provider disposals to complete
                if (disposalTasks.Count > 0)
                {
                    try
                    {
                        await Task.WhenAll(disposalTasks).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error waiting for provider disposals to complete");
                    }
                }

                Providers.Clear();
            }

            // Dispose the cancellation token source
            try
            {
                _cts?.Dispose();
                _cts = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing cancellation token source");
            }

            _logger.LogInformation("{ServiceName} asynchronous disposal completed", ServiceName);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources, false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources
                _logger.LogInformation("{ServiceName} disposing managed resources...", ServiceName);

                // Cancel any ongoing operations
                try
                {
                    _cts?.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // CancellationTokenSource was already disposed, ignore
                }

                // Dispose all providers
                if (Providers != null)
                {
                    foreach (var provider in Providers)
                    {
                        try
                        {
                            provider?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error disposing provider {Title}", provider?.Title ?? "Unknown");
                        }
                    }
                    Providers.Clear();
                }

                // Dispose the cancellation token source
                try
                {
                    _cts?.Dispose();
                    _cts = null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing cancellation token source");
                }

                _logger.LogInformation("{ServiceName} disposal completed", ServiceName);
            }

            // Free unmanaged resources (if any) here
            // Set large fields to null here (already done above for managed resources)

            _disposed = true;
        }

        /// <summary>
        /// Finalizer for AsteriskManagerService
        /// </summary>
        ~AsteriskManagerService()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <summary>
        /// Throws an ObjectDisposedException if the object has been disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        #endregion
    }
}