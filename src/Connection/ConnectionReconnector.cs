using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Connection;
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
            _lifecycleManager.OnDisconnected += HandleDisconnected;
        }

        private void HandleDisconnected(object? sender, DisconnectEventArgs e)
        {
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

            lock (_reconnectLock)
            {
                if (_isReconnecting)
                {
                    _logger.LogInformation("Reconnection attempt is already in progress.");
                    return;
                }
                _isReconnecting = true;
            }

            // Inicia a tentativa de reconexão em uma nova tarefa para não bloquear o chamador do evento
            _ = TryReconnectAsync();
        }

        private async Task TryReconnectAsync()
        {
            try
            {
                // NEW: Check if we should retry indefinitely
                if (_parameters.ReconnectRetryMax == 0)
                {
                    // Handle the infinite retry case
                    int attempt = 1;
                    while (true)
                    {
                        // Determine the delay interval based on the attempt number
                        int delayMs = (attempt <= _parameters.ReconnectRetryFast)
                            ? _parameters.ReconnectIntervalFast
                            : _parameters.ReconnectIntervalMax;

                        _logger.LogInformation("Reconnect attempt {Attempt} (infinite retries). Waiting for {Delay}ms.", attempt, delayMs);

                        await Task.Delay(delayMs);

                        try
                        {
                            _logger.LogInformation("Attempting to log in (Attempt {Attempt})...", attempt);
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                            // Use the injected authenticator to attempt login
                            await _authenticator.Login(cts.Token);

                            _logger.LogInformation("Reconnect successful!");

                            // Restart the liveness monitor for the new connection
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

                    for (int attempt = 1; attempt <= totalMaxRetries; attempt++)
                    {
                        int delayMs = (attempt <= _parameters.ReconnectRetryFast) ? _parameters.ReconnectIntervalFast : _parameters.ReconnectIntervalMax;
                        _logger.LogInformation("Reconnect attempt {Attempt}/{MaxAttempts}. Waiting for {Delay}ms.", attempt, totalMaxRetries, delayMs);

                        await Task.Delay(delayMs);

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
            _lifecycleManager.OnDisconnected -= HandleDisconnected;
        }
    }
}