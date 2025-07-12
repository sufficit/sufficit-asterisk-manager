using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The ChangeMonitorAction changes the recording filename of an active monitor session.
    /// This action allows dynamic modification of recording parameters for channels that are already being monitored.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: ChangeMonitor
    /// Purpose: Change recording filename for active monitor session
    /// Privilege Required: call,all
    /// Implementation: res/res_monitor.c
    /// Available since: Asterisk 1.0
    /// 
    /// Required Parameters:
    /// - Channel: The monitored channel (Required)
    /// - File: The new filename for recording (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Monitor Session Requirements:
    /// - Channel must already be monitored
    /// - Monitor session must be active
    /// - No effect if channel is not currently monitored
    /// - Changes apply immediately to ongoing recording
    /// 
    /// File Handling:
    /// - Changes destination filename immediately
    /// - Existing recording files remain intact
    /// - New recordings use the new filename
    /// - File format and mixing settings unchanged
    /// 
    /// Use Cases:
    /// - Dynamic filename based on call progress
    /// - Change recording location during call
    /// - Update filename with customer information
    /// - Implement time-based file naming
    /// - Organize recordings by call type
    /// - Integration with CRM systems
    /// - Compliance and audit requirements
    /// 
    /// Filename Strategies:
    /// 
    /// Customer-Based:
    /// - Include customer ID in filename
    /// - Add account number to recording name
    /// - Use caller information for organization
    /// - Implement case number tracking
    /// 
    /// Time-Based:
    /// - Change filename based on call duration
    /// - Update with timestamp information
    /// - Organize by business hours vs after-hours
    /// - Implement shift-based naming
    /// 
    /// Department-Based:
    /// - Route recordings to department folders
    /// - Add agent information to filename
    /// - Include queue or skill information
    /// - Organize by call center team
    /// 
    /// Call Control Integration:
    /// - Update filename when call is transferred
    /// - Change when escalation occurs
    /// - Modify based on IVR selections
    /// - Update with call classification
    /// 
    /// Dynamic Scenarios:
    /// - Customer identified during call
    /// - Call type determined from conversation
    /// - Priority level changes
    /// - Supervisor intervention
    /// - Compliance requirements triggered
    /// 
    /// File Management:
    /// - Maintains recording continuity
    /// - Preserves audio quality settings
    /// - Keeps mixing configuration
    /// - Retains format specifications
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.0
    /// - Consistent behavior across versions
    /// - Enhanced file handling in newer versions
    /// - Compatible with all monitor-capable channels
    /// 
    /// Implementation Notes:
    /// This action is implemented in res/res_monitor.c in Asterisk source code.
    /// Changes take effect immediately for ongoing recordings.
    /// Previous recordings with old filename remain unchanged.
    /// File paths and permissions must be valid.
    /// 
    /// Error Conditions:
    /// - Channel not currently monitored
    /// - Invalid filename or path
    /// - File system permissions issues
    /// - Channel not found or inactive
    /// - Insufficient privileges
    /// 
    /// File System Considerations:
    /// - Ensure destination directory exists
    /// - Verify write permissions
    /// - Consider disk space availability
    /// - Plan for file organization
    /// - Implement cleanup procedures
    /// 
    /// Security Considerations:
    /// - Validate filename to prevent path traversal
    /// - Ensure proper file permissions
    /// - Consider sensitive information in filenames
    /// - Monitor for unauthorized changes
    /// 
    /// Example Usage:
    /// <code>
    /// // Change to customer-specific filename
    /// var change = new ChangeMonitorAction("SIP/1001-00000001", "customer_12345_recording");
    /// 
    /// // Update with timestamp
    /// var timestamped = new ChangeMonitorAction("SIP/1001-00000001", $"call_{DateTime.Now:yyyyMMdd_HHmmss}");
    /// 
    /// // Department-based organization
    /// var departmental = new ChangeMonitorAction("SIP/1001-00000001", "support/urgent_case_789");
    /// </code>
    /// </remarks>
    /// <seealso cref="MonitorAction"/>
    /// <seealso cref="StopMonitorAction"/>
    public class ChangeMonitorAction : ManagerAction
    {
        /// <summary>
        /// Creates a new empty ChangeMonitorAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set both Channel and File
        /// properties before sending the action.
        /// </remarks>
        public ChangeMonitorAction()
        {
        }

        /// <summary>
        /// Creates a new ChangeMonitorAction for the specified channel and filename.
        /// </summary>
        /// <param name="channel">The monitored channel (Required)</param>
        /// <param name="file">The new filename for recording (Required)</param>
        /// <remarks>
        /// Channel Requirements:
        /// - Must be currently monitored
        /// - Channel must be active
        /// - Monitor session must be in progress
        /// 
        /// File Requirements:
        /// - Must be valid filename
        /// - Directory must exist and be writable
        /// - Should follow consistent naming convention
        /// - Consider file organization and cleanup
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when channel or file is null</exception>
        /// <exception cref="ArgumentException">Thrown when channel or file is empty</exception>
        public ChangeMonitorAction(string channel, string file)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentException("Channel cannot be empty", nameof(channel));
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentException("File cannot be empty", nameof(file));

            Channel = channel;
            File = file;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "ChangeMonitor"</value>
        public override string Action => "ChangeMonitor";

        /// <summary>
        /// Gets or sets the monitored channel.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The channel name that is being monitored.
        /// </value>
        /// <remarks>
        /// Channel Requirements:
        /// 
        /// Monitor Status:
        /// - Channel must already be monitored
        /// - Monitor session must be active
        /// - Recording must be in progress
        /// - No effect if channel not monitored
        /// 
        /// Channel Format Examples:
        /// - "SIP/1001-00000001" (SIP channel)
        /// - "PJSIP/1001-00000001" (PJSIP channel)
        /// - "IAX2/provider-00000001" (IAX2 channel)
        /// - "DAHDI/1-1" (DAHDI channel)
        /// - "Local/1001@from-internal-00000001;1" (Local channel)
        /// 
        /// Monitor Session Validation:
        /// - Verify channel is being monitored before changing
        /// - Check monitor session is active
        /// - Ensure channel is in appropriate state
        /// - Validate channel exists and is accessible
        /// 
        /// Channel Discovery:
        /// - Use CoreShowChannelsAction to list active channels
        /// - Monitor channel events for session tracking
        /// - Track monitor sessions from MonitorAction responses
        /// - Verify channel state before filename changes
        /// 
        /// Error Scenarios:
        /// - Channel not currently monitored: Action has no effect
        /// - Channel not found: Error response
        /// - Invalid channel format: Action failure
        /// - Channel hung up: Monitor session ended
        /// 
        /// Best Practices:
        /// - Verify monitor status before changing filename
        /// - Handle cases where monitoring may have stopped
        /// - Use consistent channel identification
        /// - Monitor events for session state changes
        /// </remarks>
        public string? Channel { get; set; }

        /// <summary>
        /// Gets or sets the new filename for the recording.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The base filename for the recording files.
        /// </value>
        /// <remarks>
        /// Filename Specifications:
        /// 
        /// Format Requirements:
        /// - Base filename without extension
        /// - Asterisk adds appropriate extensions (.wav, .gsm, etc.)
        /// - Path can include directory structure
        /// - Must be valid for the operating system
        /// 
        /// Filename Examples:
        /// 
        /// Basic Filenames:
        /// - "recording_001": Simple numbered recording
        /// - "customer_call": Descriptive name
        /// - "support_session": Department-based name
        /// 
        /// Path-Based Organization:
        /// - "recordings/2024/01/call_001": Date-organized
        /// - "customers/12345/support_call": Customer-organized
        /// - "agents/john_doe/session_789": Agent-organized
        /// - "departments/sales/lead_456": Department-organized
        /// 
        /// Dynamic Filenames:
        /// - $"customer_{customerId}_{DateTime.Now:yyyyMMdd}": Customer and date
        /// - $"agent_{agentId}_call_{callId}": Agent and call tracking
        /// - $"queue_{queueName}_{timestamp}": Queue-based organization
        /// - $"case_{caseNumber}_recording": Case-based tracking
        /// 
        /// Business Integration:
        /// - Include CRM case numbers
        /// - Add customer account information
        /// - Reference support ticket numbers
        /// - Include call classification data
        /// 
        /// File Organization Strategies:
        /// 
        /// Hierarchical Structure:
        /// - Year/Month/Day/recordings
        /// - Department/Team/Agent/calls
        /// - Customer/Account/recordings
        /// - Priority/Type/recordings
        /// 
        /// Naming Conventions:
        /// - Use consistent separators (underscore, hyphen)
        /// - Include relevant identifiers
        /// - Avoid special characters that may cause issues
        /// - Consider sorting and searching requirements
        /// 
        /// File System Considerations:
        /// - Ensure directory exists before changing filename
        /// - Verify write permissions for new location
        /// - Consider disk space in target directory
        /// - Plan for file retention and cleanup
        /// 
        /// Character Restrictions:
        /// - Avoid spaces in filenames
        /// - No special characters: / \ : * ? " < > |
        /// - Use alphanumeric and safe separators
        /// - Consider case sensitivity issues
        /// 
        /// Length Limitations:
        /// - Keep reasonable filename lengths
        /// - Consider filesystem path limits
        /// - Account for Asterisk-added extensions
        /// - Plan for future filename changes
        /// 
        /// Security Considerations:
        /// - Validate filenames to prevent path traversal
        /// - Sanitize user input used in filenames
        /// - Avoid sensitive information in filenames
        /// - Consider filename visibility in logs
        /// 
        /// Monitoring Integration:
        /// - Coordinate with file management systems
        /// - Update external tracking databases
        /// - Trigger file processing workflows
        /// - Generate audit trails for filename changes
        /// </remarks>
        public string? File { get; set; }
    }
}