using System;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The CoreShowChannelsAction displays detailed information about all active channels.
    /// This action provides comprehensive channel information including states, contexts, extensions, and more.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: CoreShowChannels
    /// Purpose: Display active channels and their current state
    /// Privilege Required: system,reporting,all
    /// 
    /// Response Flow:
    /// 1. CoreShowChannelEvent: Information about each active channel
    /// 2. CoreShowChannelsCompleteEvent: Indicates end of channel list
    /// 
    /// Channel Information Included:
    /// - Channel name and unique ID
    /// - Channel state and sub-state
    /// - Application currently executing
    /// - Application data/parameters
    /// - Caller ID information
    /// - Context, extension, and priority
    /// - Duration and elapsed time
    /// - Bridge information (if bridged)
    /// - Account code and language
    /// - Linked channel information
    /// 
    /// Usage Scenarios:
    /// - Real-time system monitoring
    /// - Call center dashboard displays
    /// - Debugging call flow issues
    /// - Channel capacity planning
    /// - Active call reporting
    /// - System health diagnostics
    /// - Call routing verification
    /// 
    /// Channel States:
    /// - "Down": Channel is down and available
    /// - "Rsrvd": Channel is reserved
    /// - "OffHook": Channel is off hook
    /// - "Dialing": Channel is dialing
    /// - "Ring": Channel is ringing
    /// - "Ringing": Channel is ringing (inbound)
    /// - "Up": Channel is answered and active
    /// - "Busy": Channel is busy
    /// - "Dialing Offhook": Channel dialing off hook
    /// - "Pre-ring": Channel in pre-ring state
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.6
    /// - Enhanced in later versions with additional channel information
    /// - Replaces older "Show Channels" command
    /// - Consistent format across modern Asterisk versions
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/cli.c in Asterisk source code.
    /// For systems with many active channels, this can generate numerous events.
    /// Consider filtering or limiting scope for high-volume systems.
    /// 
    /// Performance Considerations:
    /// - Large numbers of channels generate many events
    /// - Response time increases with channel count
    /// - Network bandwidth usage scales with activity
    /// - Consider using Status action for basic channel info
    /// 
    /// Example Response Sequence:
    /// 1. Response: Success
    /// 2. CoreShowChannelEvent (for each active channel)
    /// 3. CoreShowChannelsCompleteEvent (completion marker)
    /// 
    /// Example Usage:
    /// <code>
    /// // Get all active channels
    /// var showChannels = new CoreShowChannelsAction();
    /// 
    /// // Use with action ID for correlation
    /// var showChannels = new CoreShowChannelsAction();
    /// showChannels.ActionId = "channels_001";
    /// </code>
    /// </remarks>
    /// <seealso cref="CoreShowChannelEvent"/>
    /// <seealso cref="CoreShowChannelsCompleteEvent"/>
    /// <seealso cref="StatusAction"/>
    public class CoreShowChannelsAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "CoreShowChannels"</value>
        public override string Action => "CoreShowChannels";

        /// <summary>
        /// Returns the event type that indicates completion of the CoreShowChannels action.
        /// </summary>
        /// <returns>The Type of CoreShowChannelsCompleteEvent</returns>
        /// <remarks>
        /// The CoreShowChannelsCompleteEvent is sent by Asterisk to indicate that all
        /// channel information has been transmitted. This event marks the
        /// end of the response sequence for this action.
        /// 
        /// The completion event typically contains:
        /// - Total number of active channels
        /// - System statistics
        /// - Channel count summary
        /// </remarks>
        public override Type ActionCompleteEventClass()
        {
            return typeof(CoreShowChannelsCompleteEvent);
        }
    }
}