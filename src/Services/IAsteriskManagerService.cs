using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Sufficit.Asterisk.Manager.Services
{
    /// <summary>
    /// Interface for Asterisk Manager services that manage multiple providers.
    /// Provides common functionality for connection management and comprehensive health monitoring.
    /// Supports ASP.NET Core integration (IHealthCheck, IHostedService).
    /// </summary>
    public interface IAsteriskManagerService : IHealthCheck, IHostedService
    {
        /// <summary>
        /// Collection of AMI providers managed by this service.
        /// </summary>
        ICollection<AsteriskManagerProvider> Providers { get; }

        /// <summary>
        /// Event handler for Asterisk manager events.
        /// </summary>
        IManagerEventSubscriptions Events { get; }

        /// <summary>
        /// Last time an event was received from any provider.
        /// </summary>
        DateTimeOffset LastReceivedEvent { get; }

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
        event EventHandler<IManagerEvent>? OnManagerEvent;

        /// <summary>
        /// Starts the service (explicit declaration to avoid hiding IHostedService.StartAsync).
        /// </summary>
        new Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the service (explicit declaration to avoid hiding IHostedService.StopAsync).
        /// </summary>
        new Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Simple health check that returns the basic status for backward compatibility.
        /// </summary>
        /// <returns>Tuple with health status and message</returns>
        (bool IsHealthy, string Status) CheckHealth();

        /// <summary>
        /// Comprehensive health check that returns detailed status for all providers.
        /// </summary>
        /// <returns>Detailed health check result with provider-specific information</returns>
        AsteriskManagerHealthCheckResult CheckHealthDetailed();

        /// <summary>
        /// Asynchronous version of health check for integration with ASP.NET Core IHealthCheck.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Detailed health check result</returns>
        Task<AsteriskManagerHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Reloads the service with updated configuration.
        /// </summary>
        Task ReloadAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Extended interface for hosted Asterisk Manager services that includes IHostedService support.
    /// This interface is now DEPRECATED since IAsteriskManagerService includes IHostedService directly.
    /// </summary>
    [Obsolete("Use IAsteriskManagerService directly as it now includes IHostedService. This interface may be removed in future versions.")]
    public interface IHostedAsteriskManagerService : IAsteriskManagerService
    {
        // This interface is kept for backward compatibility but is no longer needed
        // All functionality is now in IAsteriskManagerService
    }
}