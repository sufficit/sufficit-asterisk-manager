namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The LogoffAction causes the Asterisk Manager Interface to close the connection.
    /// This action terminates the current manager session gracefully.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Logoff
    /// Purpose: Terminate the current manager connection
    /// Privilege Required: None (any authenticated connection can logoff)
    /// Available since: Early Asterisk versions
    /// 
    /// Required Parameters: None
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Logoff Behavior:
    /// - Terminates the current manager session immediately
    /// - Closes the TCP connection to the manager interface
    /// - No further actions can be sent after logoff
    /// - All pending operations may be cancelled
    /// - Connection cleanup is performed automatically
    /// 
    /// Usage Scenarios:
    /// - Clean application shutdown
    /// - Ending temporary manager sessions
    /// - Resource cleanup in connection pooling
    /// - Security compliance (explicit session termination)
    /// - Error recovery (force disconnect and reconnect)
    /// 
    /// Best Practices:
    /// - Always logoff when application terminates
    /// - Use logoff for planned disconnections
    /// - Wait for response before closing socket
    /// - Handle connection timeouts gracefully
    /// - Log session termination for audit trails
    /// 
    /// Security Considerations:
    /// - Explicit logoff prevents session hijacking
    /// - Clears any cached authentication state
    /// - Ensures proper resource cleanup
    /// - Prevents unauthorized session reuse
    /// 
    /// Alternative Disconnection:
    /// - Closing socket without logoff: May leave session active briefly
    /// - Network timeout: Less graceful than explicit logoff
    /// - Manager restart: Forcefully terminates all sessions
    /// 
    /// Response:
    /// - Success: Acknowledgment of logoff request
    /// - Connection immediately closes after response
    /// - No events are sent after logoff response
    /// 
    /// Example Usage:
    /// <code>
    /// // Basic logoff
    /// var logoff = new LogoffAction();
    /// 
    /// // Logoff with action ID for tracking
    /// var logoff = new LogoffAction();
    /// logoff.ActionId = "session_end_001";
    /// </code>
    /// </remarks>
    /// <seealso cref="LoginAction"/>
    public class LogoffAction : ManagerAction
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Logoff"</value>
        public override string Action => "Logoff";
    }
}