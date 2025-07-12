using System;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The StatusAction requests the state of all active channels in Asterisk.
    /// This action provides comprehensive information about channel status and statistics.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Status
    /// Purpose: Retrieve detailed information about all active channels
    /// Privilege Required: system,call,all
    /// 
    /// Response Flow:
    /// 1. StatusEvent: Information about each active channel
    /// 2. StatusCompleteEvent: Indicates end of status dump
    /// 
    /// Channel Information Included:
    /// - Channel name and unique ID
    /// - Channel state (Up, Down, Rsrvd, OffHook, Dialing, Ring, Ringing, etc.)
    /// - Caller ID information
    /// - Connected line information
    /// - Account code and context
    /// - Dialplan location (context, extension, priority)
    /// - Bridge information (if bridged)
    /// - Application currently executing
    /// - Duration and time information
    /// 
    /// Usage scenarios:
    /// - System monitoring and diagnostics
    /// - Call center reporting
    /// - Real-time channel tracking
    /// - System health checks
    /// - Debugging call flow issues
    /// 
    /// Asterisk Versions:
    /// - Available since early Asterisk versions
    /// - StatusCompleteEvent added to mark completion
    /// - Enhanced with additional channel fields in newer versions
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// For systems with many active channels, this can generate significant events.
    /// Consider using channel-specific queries for targeted monitoring.
    /// 
    /// Example Response Sequence:
    /// 1. Response: Success
    /// 2. StatusEvent (for each active channel)
    /// 3. StatusCompleteEvent (completion marker)
    /// </remarks>
    /// <seealso cref="StatusEvent"/>
    /// <seealso cref="StatusCompleteEvent"/>
    public class StatusAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Status"</value>
        public override string Action => "Status";

        /// <summary>
        /// Returns the event type that indicates completion of the Status action.
        /// </summary>
        /// <returns>The Type of StatusCompleteEvent</returns>
        /// <remarks>
        /// The StatusCompleteEvent is sent by Asterisk to indicate that all
        /// channel status information has been transmitted. This event marks the
        /// end of the response sequence for this action.
        /// 
        /// The completion event contains summary statistics:
        /// - Total number of active channels
        /// - Total number of active calls
        /// - System uptime information
        /// </remarks>
        public override Type ActionCompleteEventClass()
        {
            return typeof(StatusCompleteEvent);
        }
    }
}