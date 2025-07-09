using System;

namespace Sufficit.Asterisk.Manager.Services
{
    /// <summary>
    /// Health information for an individual Asterisk Manager provider.
    /// Contains detailed status information about a specific provider's connection and authentication state.
    /// </summary>
    /// <remarks>
    /// This class provides comprehensive health monitoring for individual providers within an
    /// AsteriskManagerService. It tracks connection state, authentication status, and any
    /// error conditions that may affect the provider's ability to communicate with Asterisk.
    /// 
    /// The health information is used for:
    /// - Overall service health assessment
    /// - Troubleshooting connection issues
    /// - Monitoring and alerting systems
    /// - Health check endpoints and dashboards
    /// </remarks>
    public class ProviderHealthInfo
    {
        /// <summary>
        /// Whether this provider is healthy (connected and authenticated).
        /// A provider is considered healthy only when it has an active connection
        /// that is both connected and authenticated with the Asterisk server.
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Provider title/name for identification.
        /// This is the friendly name assigned to the provider for logging and display purposes.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Server address this provider connects to.
        /// The hostname or IP address of the Asterisk server this provider manages.
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Current connection status message.
        /// Human-readable description of the provider's current state, such as:
        /// - "Connected and authenticated"
        /// - "Connection not connected"
        /// - "Connection not authenticated"
        /// - "Connection unavailable"
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Whether connection object is available.
        /// Indicates if the provider has a connection object instantiated,
        /// regardless of its current connection or authentication state.
        /// </summary>
        public bool HasConnection { get; set; }

        /// <summary>
        /// Whether the connection is actively connected to the Asterisk server.
        /// This indicates the TCP connection state, but not authentication status.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Whether the connection is authenticated with the Asterisk server.
        /// A connection must be both connected and authenticated to be considered healthy.
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Last known exception or error message.
        /// Contains details about the most recent error that affected this provider,
        /// useful for troubleshooting connection or authentication issues.
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// Port number used for connection to the Asterisk server.
        /// Default AMI port is typically 5038.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Username used for authentication with the Asterisk server.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Timestamp of when this health information was last updated.
        /// </summary>
        public DateTimeOffset LastUpdated { get; set; }

        /// <summary>
        /// Duration since the last successful connection.
        /// Null if never connected or currently connected.
        /// </summary>
        public TimeSpan? TimeSinceLastConnection { get; set; }

        /// <summary>
        /// Number of connection attempts since the last successful connection.
        /// Reset to 0 when connection is established.
        /// </summary>
        public int ConnectionAttempts { get; set; }

        /// <summary>
        /// Initializes a new instance of ProviderHealthInfo with default values.
        /// </summary>
        public ProviderHealthInfo()
        {
            LastUpdated = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets a detailed status description including connection and authentication state.
        /// </summary>
        /// <returns>Comprehensive status string for logging or display</returns>
        public string GetDetailedStatus()
        {
            if (!HasConnection)
                return $"No connection object available for {Title}";

            if (!IsConnected)
                return $"Not connected to {Address}:{Port}";

            if (!IsAuthenticated)
                return $"Connected to {Address}:{Port} but not authenticated";

            return $"Connected and authenticated to {Address}:{Port} as {Username}";
        }

        /// <summary>
        /// Determines the overall health level of this provider.
        /// </summary>
        /// <returns>Health level enum indicating severity of any issues</returns>
        public HealthLevel GetHealthLevel()
        {
            if (IsHealthy)
                return HealthLevel.Healthy;

            if (!HasConnection)
                return HealthLevel.Critical;

            if (!IsConnected)
                return HealthLevel.Unhealthy;

            if (!IsAuthenticated)
                return HealthLevel.Warning;

            return HealthLevel.Unknown;
        }

        /// <summary>
        /// Updates the timestamp to indicate when this health information was refreshed.
        /// </summary>
        public void MarkAsUpdated()
        {
            LastUpdated = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Enumeration of health levels for provider status classification.
    /// </summary>
    public enum HealthLevel
    {
        /// <summary>
        /// Provider is fully operational (connected and authenticated).
        /// </summary>
        Healthy,

        /// <summary>
        /// Provider has minor issues but may still be functional.
        /// </summary>
        Warning,

        /// <summary>
        /// Provider has significant issues affecting functionality.
        /// </summary>
        Unhealthy,

        /// <summary>
        /// Provider has critical issues and is not functional.
        /// </summary>
        Critical,

        /// <summary>
        /// Provider health status cannot be determined.
        /// </summary>
        Unknown
    }
}