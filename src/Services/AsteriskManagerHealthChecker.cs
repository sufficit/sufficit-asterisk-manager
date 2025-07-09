using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager.Services
{
    /// <summary>
    /// Health check manager for Asterisk Manager services.
    /// Provides centralized health monitoring logic that can be used by AsteriskManagerService
    /// and other health monitoring components.
    /// </summary>
    /// <remarks>
    /// This class encapsulates the health check logic to promote reusability and testability.
    /// It can perform both basic and comprehensive health assessments of Asterisk Manager providers,
    /// and convert results to various formats including ASP.NET Core HealthCheckResult.
    /// 
    /// Key features:
    /// - Provider-level health assessment
    /// - Configurable health thresholds
    /// - Integration with ASP.NET Core health checks
    /// - Extensible for business-specific health logic
    /// - Comprehensive logging and diagnostics
    /// </remarks>
    public class AsteriskManagerHealthChecker
    {
        private readonly ILogger<AsteriskManagerHealthChecker> _logger;
        private readonly HealthCheckConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of AsteriskManagerHealthChecker.
        /// </summary>
        /// <param name="logger">Logger for health check operations</param>
        /// <param name="configuration">Health check configuration settings</param>
        public AsteriskManagerHealthChecker(
            ILogger<AsteriskManagerHealthChecker> logger,
            HealthCheckConfiguration? configuration = null)
        {
            _logger = logger;
            _configuration = configuration ?? new HealthCheckConfiguration();
        }

        /// <summary>
        /// Performs a comprehensive health check of the provided Asterisk Manager providers.
        /// </summary>
        /// <param name="providers">Collection of providers to assess</param>
        /// <param name="lastReceivedEvent">Timestamp of the last received event</param>
        /// <param name="extendedDataProvider">Optional delegate to provide additional health data</param>
        /// <returns>Comprehensive health check result</returns>
        public AsteriskManagerHealthCheckResult CheckHealth(
            ICollection<AsteriskManagerProvider> providers,
            DateTimeOffset lastReceivedEvent,
            Func<Dictionary<string, object>?>? extendedDataProvider = null)
        {
            _logger.LogDebug("Starting health check for {ProviderCount} providers", providers.Count);

            var result = new AsteriskManagerHealthCheckResult
            {
                LastReceivedEvent = lastReceivedEvent,
                TotalProviders = providers.Count
            };

            var unhealthyProviders = new List<string>();
            var healthyCount = 0;

            // Assess each provider's health
            foreach (var provider in providers)
            {
                var providerHealth = AssessProviderHealth(provider);
                result.ProvidersHealth[provider.Title] = providerHealth;

                if (providerHealth.IsHealthy)
                {
                    healthyCount++;
                }
                else
                {
                    unhealthyProviders.Add(provider.Title);
                    _logger.LogWarning("Provider {Title} is unhealthy: {Status}", 
                        provider.Title, providerHealth.Status);
                }
            }

            // Calculate overall health metrics
            result.HealthyProviders = healthyCount;
            result.UnhealthyProviders = providers.Count - healthyCount;

            // Determine overall health status
            result.IsHealthy = DetermineOverallHealth(result, unhealthyProviders);

            // Generate status message
            result.Status = GenerateStatusMessage(result, unhealthyProviders, lastReceivedEvent);

            // Include extended data if provided
            if (extendedDataProvider != null)
            {
                try
                {
                    var extendedData = extendedDataProvider();
                    if (extendedData != null)
                    {
                        foreach (var kvp in extendedData)
                        {
                            result.ExtendedData[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error retrieving extended health data");
                    result.ExtendedData["ExtendedDataError"] = ex.Message;
                }
            }

            _logger.LogDebug("Health check completed: {IsHealthy}, {HealthyCount}/{TotalCount} providers healthy",
                result.IsHealthy, result.HealthyProviders, result.TotalProviders);

            return result;
        }

        /// <summary>
        /// Performs an asynchronous health check with support for async extended data providers.
        /// </summary>
        /// <param name="providers">Collection of providers to assess</param>
        /// <param name="lastReceivedEvent">Timestamp of the last received event</param>
        /// <param name="extendedDataProvider">Optional async delegate to provide additional health data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Comprehensive health check result</returns>
        public async Task<AsteriskManagerHealthCheckResult> CheckHealthAsync(
            ICollection<AsteriskManagerProvider> providers,
            DateTimeOffset lastReceivedEvent,
            Func<Task<Dictionary<string, object>?>>? extendedDataProvider = null,
            CancellationToken cancellationToken = default)
        {
            // Perform synchronous health check first
            var result = CheckHealth(providers, lastReceivedEvent);

            // Add async extended data if provider is available
            if (extendedDataProvider != null)
            {
                try
                {
                    var extendedData = await extendedDataProvider().ConfigureAwait(false);
                    if (extendedData != null)
                    {
                        foreach (var kvp in extendedData)
                        {
                            result.ExtendedData[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error retrieving async extended health data");
                    result.ExtendedData["AsyncExtendedDataError"] = ex.Message;
                }
            }

            return result;
        }

        /// <summary>
        /// Converts the internal health check result to ASP.NET Core HealthCheckResult format.
        /// </summary>
        /// <param name="healthResult">Internal health check result</param>
        /// <returns>ASP.NET Core HealthCheckResult</returns>
        public Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult ToAspNetHealthCheckResult(
            AsteriskManagerHealthCheckResult healthResult)
        {
            var data = new Dictionary<string, object>
            {
                ["total_providers"] = healthResult.TotalProviders,
                ["healthy_providers"] = healthResult.HealthyProviders,
                ["unhealthy_providers"] = healthResult.UnhealthyProviders,
                ["last_received_event"] = healthResult.LastReceivedEvent,
                ["providers_health"] = healthResult.ProvidersHealth,
                ["provider_summary"] = healthResult.GetProviderSummary()
            };

            // Include extended data
            foreach (var item in healthResult.ExtendedData)
            {
                data[item.Key] = item.Value;
            }

            return healthResult.IsHealthy
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(healthResult.Status, data)
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(healthResult.Status, data: data);
        }

        /// <summary>
        /// Assesses the health of an individual provider.
        /// </summary>
        /// <param name="provider">Provider to assess</param>
        /// <returns>Detailed health information for the provider</returns>
        private ProviderHealthInfo AssessProviderHealth(AsteriskManagerProvider provider)
        {
            var providerHealth = new ProviderHealthInfo
            {
                Title = provider.Title,
                Address = provider.Options.Address,
                Port = (int)provider.Options.Port, // Convert uint to int
                Username = provider.Options.Username,
                HasConnection = provider.Connection != null
            };

            if (provider.Connection == null)
            {
                providerHealth.IsHealthy = false;
                providerHealth.Status = "Connection unavailable";
            }
            else
            {
                providerHealth.IsConnected = provider.Connection.IsConnected;
                providerHealth.IsAuthenticated = provider.Connection.IsAuthenticated;

                if (!provider.Connection.IsConnected)
                {
                    providerHealth.IsHealthy = false;
                    providerHealth.Status = "Connection not connected";
                }
                else if (!provider.Connection.IsAuthenticated)
                {
                    providerHealth.IsHealthy = false;
                    providerHealth.Status = "Connection not authenticated";
                }
                else
                {
                    providerHealth.IsHealthy = true;
                    providerHealth.Status = "Connected and authenticated";
                }
            }

            providerHealth.MarkAsUpdated();
            return providerHealth;
        }

        /// <summary>
        /// Determines overall health based on provider health and configuration thresholds.
        /// </summary>
        /// <param name="result">Health check result</param>
        /// <param name="unhealthyProviders">List of unhealthy provider names</param>
        /// <returns>True if service is considered healthy overall</returns>
        private bool DetermineOverallHealth(
            AsteriskManagerHealthCheckResult result,
            List<string> unhealthyProviders)
        {
            // No providers configured is unhealthy
            if (result.TotalProviders == 0)
                return false;

            // Apply configuration thresholds
            var healthyPercentage = (double)result.HealthyProviders / result.TotalProviders;

            switch (_configuration.HealthThreshold)
            {
                case HealthThreshold.AllProviders:
                    return unhealthyProviders.Count == 0;

                case HealthThreshold.MajorityProviders:
                    return healthyPercentage > 0.5;

                case HealthThreshold.AtLeastOneProvider:
                    return result.HealthyProviders > 0;

                case HealthThreshold.MinimumPercentage:
                    return healthyPercentage >= _configuration.MinimumHealthyPercentage;

                default:
                    return unhealthyProviders.Count == 0;
            }
        }

        /// <summary>
        /// Generates a human-readable status message based on health results.
        /// </summary>
        /// <param name="result">Health check result</param>
        /// <param name="unhealthyProviders">List of unhealthy provider names</param>
        /// <param name="lastReceivedEvent">Last event timestamp</param>
        /// <returns>Formatted status message</returns>
        private string GenerateStatusMessage(
            AsteriskManagerHealthCheckResult result,
            List<string> unhealthyProviders,
            DateTimeOffset lastReceivedEvent)
        {
            if (result.IsHealthy)
            {
                var eventInfo = lastReceivedEvent == DateTimeOffset.MinValue
                    ? "No events received yet"
                    : $"Last event: {lastReceivedEvent:yyyy-MM-dd HH:mm:ss} UTC";

                return $"All {result.TotalProviders} providers healthy. {eventInfo}";
            }
            else
            {
                var unhealthyList = string.Join(", ", unhealthyProviders.Take(5));
                var moreCount = unhealthyProviders.Count - 5;
                var truncateInfo = moreCount > 0 ? $" and {moreCount} more" : "";

                return $"{result.UnhealthyProviders}/{result.TotalProviders} providers unhealthy: {unhealthyList}{truncateInfo}";
            }
        }
    }

    /// <summary>
    /// Configuration options for health check behavior.
    /// </summary>
    public class HealthCheckConfiguration
    {
        /// <summary>
        /// Threshold for determining overall service health.
        /// </summary>
        public HealthThreshold HealthThreshold { get; set; } = HealthThreshold.AllProviders;

        /// <summary>
        /// Minimum percentage of healthy providers required when using MinimumPercentage threshold.
        /// </summary>
        public double MinimumHealthyPercentage { get; set; } = 0.8;

        /// <summary>
        /// Maximum age of last received event before considering service stale.
        /// </summary>
        public TimeSpan MaxEventAge { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to include detailed provider information in health check results.
        /// </summary>
        public bool IncludeDetailedProviderInfo { get; set; } = true;
    }

    /// <summary>
    /// Enumeration of health thresholds for service health determination.
    /// </summary>
    public enum HealthThreshold
    {
        /// <summary>
        /// All providers must be healthy for service to be considered healthy.
        /// </summary>
        AllProviders,

        /// <summary>
        /// Majority of providers must be healthy for service to be considered healthy.
        /// </summary>
        MajorityProviders,

        /// <summary>
        /// At least one provider must be healthy for service to be considered healthy.
        /// </summary>
        AtLeastOneProvider,

        /// <summary>
        /// A minimum percentage of providers must be healthy.
        /// </summary>
        MinimumPercentage
    }
}