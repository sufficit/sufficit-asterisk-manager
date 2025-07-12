using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The StopMonitorAction ends monitoring (recording) on a specific channel.
    /// This action stops call recording initiated by MonitorAction.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: StopMonitor
    /// Purpose: Stop call recording on a specific channel
    /// Privilege Required: call,all
    /// Implementation: res/res_monitor.c
    /// Available since: Asterisk 1.0
    /// 
    /// Required Parameters:
    /// - Channel: The channel to stop monitoring (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Stop Behavior:
    /// - Immediately stops recording on the specified channel
    /// - Closes any open recording files
    /// - Triggers file mixing if Mix was enabled in MonitorAction
    /// - Generates monitor stop events
    /// 
    /// File Handling:
    /// - Closes input and output recording files
    /// - Finalizes file headers and metadata
    /// - Triggers post-processing if configured
    /// - Moves files to final location if configured
    /// 
    /// Mix Processing:
    /// - If Mix=true was set in MonitorAction, mixing begins after stop
    /// - Uses sox utility to combine input and output streams
    /// - Creates single mixed file from separate stream files
    /// - Original separate files may be deleted after successful mixing
    /// 
    /// Usage Scenarios:
    /// - End of call recording
    /// - Manual recording termination
    /// - Emergency recording stop
    /// - Selective recording control
    /// - Compliance requirement fulfillment
    /// - Storage space management
    /// - Call completion processing
    /// 
    /// Recording Lifecycle:
    /// 1. MonitorAction starts recording
    /// 2. Audio streams recorded to files
    /// 3. StopMonitorAction ends recording
    /// 4. Files closed and finalized
    /// 5. Optional mixing process
    /// 6. Files ready for playback/archival
    /// 
    /// Error Conditions:
    /// - Channel not currently being monitored
    /// - Invalid channel specification
    /// - File system errors during file closure
    /// - Insufficient privileges
    /// - Channel no longer exists
    /// 
    /// Integration Notes:
    /// - Works with any channel type that supports monitoring
    /// - Can be called on channels in any state
    /// - Does not affect call progression
    /// - Safe to call multiple times on same channel
    /// 
    /// Storage Considerations:
    /// - Ensures all buffered audio is written to disk
    /// - Finalizes file timestamps and metadata
    /// - Triggers any configured post-processing
    /// - Updates file permissions if configured
    /// 
    /// Alternative Recording Methods:
    /// - MixMonitor application (newer, more flexible)
    /// - RECORD dialplan application  
    /// - Channel-specific recording features
    /// - Third-party recording solutions
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.0
    /// - Consistent behavior across versions
    /// - Enhanced file handling in newer versions
    /// - Compatible with all monitor-capable channels
    /// 
    /// Implementation Notes:
    /// This action is implemented in res/res_monitor.c in Asterisk source code.
    /// Stop operation is synchronous and completes before response.
    /// File operations may continue briefly after response.
    /// 
    /// Performance Impact:
    /// - Minimal CPU overhead for stop operation
    /// - Brief disk I/O for file finalization
    /// - Mixing process (if enabled) runs in background
    /// - Memory freed immediately after stop
    /// 
    /// Example Usage:
    /// <code>
    /// // Stop monitoring on a specific channel
    /// var stop = new StopMonitorAction("SIP/1001-00000001");
    /// 
    /// // Stop with action ID for tracking
    /// var stop = new StopMonitorAction("SIP/1001-00000001");
    /// stop.ActionId = "stop_001";
    /// </code>
    /// </remarks>
    /// <seealso cref="MonitorAction"/>
    /// <seealso cref="ChangeMonitorAction"/>
    public class StopMonitorAction : ManagerAction
    {
        /// <summary>
        /// Creates a new empty StopMonitorAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set the Channel property
        /// before sending the action.
        /// </remarks>
        public StopMonitorAction()
        {
        }

        /// <summary>
        /// Creates a new StopMonitorAction for the specified channel.
        /// </summary>
        /// <param name="channel">The name of the channel to stop monitoring (Required)</param>
        /// <remarks>
        /// Channel Requirements:
        /// - Must be an active channel currently being monitored
        /// - Channel name must exactly match the one used in MonitorAction
        /// - Channel format: "Technology/Resource-UniqueID"
        /// 
        /// Channel Examples:
        /// - "SIP/1001-00000001" (SIP channel)
        /// - "IAX2/provider-00000001" (IAX2 channel)  
        /// - "DAHDI/1-1" (DAHDI channel)
        /// - "Local/1001@from-internal-00000001;1" (Local channel)
        /// - "PJSIP/1001-00000001" (PJSIP channel)
        /// 
        /// Channel State Considerations:
        /// - Channel can be in any state (Up, Ring, etc.)
        /// - Works on bridged or unbridged channels
        /// - Safe to call on channels about to hang up
        /// - No effect if channel not currently monitored
        /// 
        /// Monitoring Status:
        /// - Only channels started with MonitorAction can be stopped
        /// - Multiple StopMonitor calls on same channel are safe
        /// - Channel must still exist when action is executed
        /// - Recording files are finalized regardless of channel state
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when channel is null</exception>
        /// <exception cref="ArgumentException">Thrown when channel is empty</exception>
        public StopMonitorAction(string channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentException("Channel cannot be empty", nameof(channel));

            Channel = channel;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "StopMonitor"</value>
        public override string Action => "StopMonitor";

        /// <summary>
        /// Gets or sets the name of the channel to stop monitoring.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The channel name to stop monitoring.
        /// </value>
        /// <remarks>
        /// Channel Identification:
        /// 
        /// Format Requirements:
        /// - Must use complete channel name with unique identifier
        /// - Format: "Technology/Resource-UniqueID"
        /// - Case-sensitive exact match required
        /// - Must match channel name from original MonitorAction
        /// 
        /// Channel Technology Examples:
        /// 
        /// SIP Channels:
        /// - "SIP/1001-00000001" (SIP peer call)
        /// - Format: SIP/peer-uniqueid
        /// - UniqueID generated by Asterisk for each call
        /// 
        /// PJSIP Channels:
        /// - "PJSIP/1001-00000001" (PJSIP endpoint call)
        /// - Format: PJSIP/endpoint-uniqueid
        /// - Modern SIP implementation in Asterisk
        /// 
        /// IAX2 Channels:
        /// - "IAX2/provider/5551234567-00000001" (IAX2 trunk call)
        /// - Format: IAX2/peer[/number]-uniqueid
        /// - May include dialed number in channel name
        /// 
        /// DAHDI Channels:
        /// - "DAHDI/1-1" (DAHDI channel 1, call 1)
        /// - Format: DAHDI/channel-callnumber
        /// - Hardware-based telephony channels
        /// 
        /// Local Channels:
        /// - "Local/1001@from-internal-00000001;1" (Local channel leg 1)
        /// - "Local/1001@from-internal-00000001;2" (Local channel leg 2)
        /// - Format: Local/extension@context-uniqueid;leg
        /// - Used for internal call routing
        /// 
        /// Channel Discovery:
        /// - Use CoreShowChannelsAction to list active channels
        /// - Monitor channel events for channel names
        /// - Store channel names from MonitorAction responses
        /// - Use StatusAction for individual channel info
        /// 
        /// Channel State Considerations:
        /// - Channel must exist when StopMonitor is executed
        /// - Channel can be in any state (Up, Ring, Down, etc.)
        /// - Monitoring can be stopped on bridged channels
        /// - Safe to call on channels that will hang up soon
        /// 
        /// Monitoring Validation:
        /// - Only channels with active monitoring can be stopped
        /// - No error if channel not currently monitored
        /// - Multiple stop calls on same channel are harmless
        /// - Verify monitoring status before stop if needed
        /// 
        /// Error Scenarios:
        /// - Channel name not found
        /// - Channel already hung up
        /// - Invalid channel name format
        /// - Channel not currently monitored
        /// - File system errors during stop
        /// 
        /// Best Practices:
        /// - Store channel names when starting monitoring
        /// - Verify channel exists before stopping
        /// - Handle cases where channel may have hung up
        /// - Use consistent channel naming across operations
        /// - Monitor events for channel state changes
        /// </remarks>
        public string? Channel { get; set; }
    }
}