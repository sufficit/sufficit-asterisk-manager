using System;
using System.Collections.Generic;
using System.Linq;

namespace Sufficit.Asterisk.Manager.Services
{
    /// <summary>
    /// Comprehensive health check result containing status, message, and detailed provider information.
    /// Used by AsteriskManagerService and derived classes to provide detailed health monitoring.
    /// </summary>
    /// <remarks>
    /// This class represents the complete health status of an AsteriskManagerService instance,
    /// including detailed information about each provider's health state, overall service status,
    /// and extended data for business-specific health indicators.
    /// 
    /// The result can be easily converted to ASP.NET Core HealthCheckResult format for
    /// integration with the built-in health check infrastructure.
    /// </remarks>
    public class AsteriskManagerHealthCheckResult
    {
        /// <summary>
        /// Overall health status of the service.
        /// True if all providers are healthy and service is operating normally.
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Human-readable status message summarizing the current health state.
        /// Includes provider counts and last event timestamp for healthy services,
        /// or error details for unhealthy services.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Detailed information about each provider's health status.
        /// Key is the provider title, value contains comprehensive health information.
        /// </summary>
        public Dictionary<string, ProviderHealthInfo> ProvidersHealth { get; set; } = new();

        /// <summary>
        /// Last time an event was received from any provider.
        /// Used to detect service activity and potential connection issues.
        /// </summary>
        public DateTimeOffset LastReceivedEvent { get; set; }

        /// <summary>
        /// Total number of configured providers in the service.
        /// </summary>
        public int TotalProviders { get; set; }

        /// <summary>
        /// Number of healthy (connected and authenticated) providers.
        /// </summary>
        public int HealthyProviders { get; set; }

        /// <summary>
        /// Number of unhealthy (disconnected or unavailable) providers.
        /// </summary>
        public int UnhealthyProviders { get; set; }

        /// <summary>
        /// Additional data that can be used by derived classes for extended health checks.
        /// This dictionary allows business-specific health indicators to be included
        /// in the overall health assessment.
        /// </summary>
        /// <example>
        /// result.ExtendedData["ActiveCalls"] = GetActiveCallCount();
        /// result.ExtendedData["QueueStatus"] = GetQueueHealthStatus();
        /// result.ExtendedData["DatabaseConnected"] = IsDatabaseConnected();
        /// </example>
        public Dictionary<string, object> ExtendedData { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of AsteriskManagerHealthCheckResult with default values.
        /// </summary>
        public AsteriskManagerHealthCheckResult()
        {
            LastReceivedEvent = DateTimeOffset.MinValue;
        }

        /// <summary>
        /// Gets a summary of provider health for logging or display purposes.
        /// </summary>
        /// <returns>A formatted string showing provider health statistics</returns>
        public string GetProviderSummary()
        {
            return $"{HealthyProviders}/{TotalProviders} providers healthy";
        }

        /// <summary>
        /// Determines if the service has recent activity based on the last received event.
        /// </summary>
        /// <param name="maxIdleTime">Maximum time since last event to consider service active</param>
        /// <returns>True if service has recent activity, false otherwise</returns>
        public bool HasRecentActivity(TimeSpan maxIdleTime)
        {
            if (LastReceivedEvent == DateTimeOffset.MinValue)
                return false;

            return DateTimeOffset.UtcNow - LastReceivedEvent <= maxIdleTime;
        }

        /// <summary>
        /// Gets all unhealthy provider titles for error reporting.
        /// </summary>
        /// <returns>Collection of titles of providers that are not healthy</returns>
        public IEnumerable<string> GetUnhealthyProviderTitles()
        {
            return ProvidersHealth
                .Where(kvp => !kvp.Value.IsHealthy)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Creates a simplified health check result for backward compatibility.
        /// </summary>
        /// <returns>Tuple containing basic health status and message</returns>
        public (bool IsHealthy, string Status) ToSimpleResult()
        {
            return (IsHealthy, Status);
        }
    }
}