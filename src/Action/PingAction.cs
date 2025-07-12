namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The PingAction sends a ping request to the Asterisk server to test connectivity.
    /// This action elicits a 'Pong' response and is used to keep the manager connection alive.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Ping
    /// Purpose: Test connection and keep session alive
    /// Privilege Required: None (basic connectivity test)
    /// Available since: Early Asterisk versions
    /// 
    /// Required Parameters: None
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Response:
    /// - "Pong" response indicating successful connectivity
    /// - Timestamp of the ping/pong exchange
    /// - Action ID correlation if provided
    /// 
    /// Usage Scenarios:
    /// - Connection health monitoring
    /// - Keep-alive mechanism for long-running connections
    /// - Network latency testing
    /// - Connection timeout prevention
    /// - Heartbeat implementation
    /// - Connection validation before critical operations
    /// 
    /// Keep-Alive Benefits:
    /// - Prevents connection timeouts
    /// - Detects network interruptions quickly
    /// - Maintains session state
    /// - Avoids reconnection overhead
    /// - Ensures manager interface availability
    /// 
    /// Implementation Patterns:
    /// - Periodic ping (every 30-60 seconds)
    /// - Pre-operation connectivity check
    /// - Network monitoring integration
    /// - Failover detection mechanism
    /// - Load balancer health checks
    /// 
    /// Performance Considerations:
    /// - Very low overhead operation
    /// - No server-side processing required
    /// - Minimal network bandwidth usage
    /// - Fast response time (< 10ms typically)
    /// - Safe for high-frequency use
    /// 
    /// Network Monitoring:
    /// - Measure round-trip time
    /// - Detect packet loss
    /// - Monitor connection stability
    /// - Track manager interface responsiveness
    /// - Identify network degradation
    /// 
    /// Asterisk Versions:
    /// - Available since early Asterisk versions
    /// - Consistent behavior across all versions
    /// - No version-specific variations
    /// - Reliable cross-platform operation
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// The response is immediate and requires no authentication.
    /// Can be sent on any authenticated manager connection.
    /// Does not affect system state or configuration.
    /// 
    /// Error Conditions:
    /// - Connection lost (timeout)
    /// - Network interruption
    /// - Server overload (delayed response)
    /// - Manager interface disabled
    /// 
    /// Example Usage:
    /// <code>
    /// // Basic ping
    /// var ping = new PingAction();
    /// 
    /// // Ping with action ID for tracking
    /// var ping = new PingAction();
    /// ping.ActionId = "ping_001";
    /// 
    /// // Measure response time
    /// var start = DateTime.UtcNow;
    /// var response = await connection.SendActionAsync(ping);
    /// var latency = DateTime.UtcNow - start;
    /// </code>
    /// </remarks>
    /// <seealso cref="LoginAction"/>
    /// <seealso cref="LogoffAction"/>
    public class PingAction : ManagerAction
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Ping"</value>
        public override string Action => "Ping";
    }
}