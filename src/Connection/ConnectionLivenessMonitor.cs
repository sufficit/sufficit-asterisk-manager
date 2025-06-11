using AsterNET.Manager.Action;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager.Connection
{
    /// <summary>
    /// Monitors the connection's liveness by sending periodic PingActions
    /// during periods of inactivity.
    /// </summary>
    public class ConnectionLivenessMonitor : IDisposable
    {
        private static readonly ILogger _logger = ManagerLogger.CreateLogger<ConnectionLivenessMonitor>();

        private readonly IAMISocketManager _lifecycleManager;
        private readonly IActionDispatcher _actionDispatcher;
        private readonly int _pingInterval;

        /// <summary>
        /// CancellationTokenSource to control the liveness monitor background task.
        /// </summary>
        private CancellationTokenSource? _monitorCts;

        /// <summary>
        ///     Tracks the timestamp of the last message received from Asterisk to detect idle connections.
        /// </summary>
        public DateTime LastMessageReceived { get; set; }

        public ConnectionLivenessMonitor(ManagerConnectionParameters parameters, IAMISocketManager lifecycleManager, IActionDispatcher actionDispatcher)
        {
            _lifecycleManager = lifecycleManager;
            _actionDispatcher = actionDispatcher;
            _pingInterval = parameters.PingInterval.HasValue ? (int)parameters.PingInterval.Value : 10000;

            // This monitor's start/stop is tied to the authenticator's state.
            // A more advanced implementation would use events from the authenticator.
            // For now, we assume it's started/stopped by the main ManagerConnection.

            // Simplified: We listen to the raw disconnect.
            _lifecycleManager.OnDisconnected += (s, e) => Stop();
        }

        public void Start()
        {
            if (_monitorCts != null) return;
            _logger.LogInformation("Starting connection liveness monitor. Ping interval: {PingInterval}ms", _pingInterval);
            _monitorCts = new CancellationTokenSource();
            LastMessageReceived = DateTime.UtcNow;
            _ = MonitorLivenessAsync(_monitorCts.Token);
        }

        public void Stop()
        {
            if (_monitorCts == null) return;
            _logger.LogInformation("Stopping connection liveness monitor.");
            _monitorCts.Cancel();
            _monitorCts.Dispose();
            _monitorCts = null;
        }

        private async Task MonitorLivenessAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_pingInterval, cancellationToken);
                    if (cancellationToken.IsCancellationRequested) break;

                    if ((DateTime.UtcNow - LastMessageReceived).TotalMilliseconds >= _pingInterval)
                    {
                        _logger.LogDebug("Connection idle. Sending PingAction.");
                        // Send a ping and expect a pong response to update LastMessageReceived
                        await _actionDispatcher.SendActionAsync<ManagerResponseEvent>(new PingAction(), cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break; // Expected when stopping
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Liveness monitor encountered an error. Disconnecting.");
                    _lifecycleManager.Disconnect("Liveness monitor failed.", isPermanent: false);
                    break;
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
