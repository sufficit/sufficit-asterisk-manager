using System;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The QueueStatus action requests the state of queues, their members (agents), and entries (callers).
    /// This action provides comprehensive information about call queue operations.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: QueueStatus
    /// Purpose: Retrieve detailed information about call queues
    /// Privilege Required: system,call,all
    /// 
    /// Response Flow:
    /// 1. QueueParams events: Basic queue configuration and statistics
    /// 2. QueueMember events: Information about each queue member (agent)
    /// 3. QueueEntry events: Information about each caller in queue
    /// 4. QueueStatusComplete event: Indicates end of status dump
    /// 
    /// Optional Filters:
    /// - Queue: Limit results to specific queue name
    /// - Member: Limit results to specific member
    /// 
    /// Usage scenarios:
    /// - Call center monitoring and reporting
    /// - Queue performance analysis
    /// - Agent status tracking
    /// - Real-time queue diagnostics
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.2
    /// - QueueStatusComplete event added in Asterisk 1.2
    /// - Enhanced with additional fields in later versions
    /// 
    /// Implementation Notes:
    /// This action is implemented in apps/app_queue.c in Asterisk source code.
    /// The response can generate a large number of events for busy call centers.
    /// </remarks>
    /// <seealso cref="QueueParamsEvent"/>
    /// <seealso cref="QueueMemberEvent"/>
    /// <seealso cref="QueueEntryEvent"/>
    /// <seealso cref="QueueStatusCompleteEvent"/>
    public class QueueStatusAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "QueueStatus"</value>
        public override string Action => "QueueStatus";

        /// <summary>
        /// Gets or sets the queue name filter.
        /// When specified, only information about the named queue is returned.
        /// </summary>
        /// <value>
        /// The queue name to filter by, or null to retrieve all queues.
        /// </value>
        /// <remarks>
        /// When this property is set, the response will be limited to:
        /// - Events related only to the specified queue
        /// - Faster response time for targeted queries
        /// - Reduced network traffic and processing overhead
        /// 
        /// Example queue names: "sales", "support", "callback"
        /// </remarks>
        public string? Queue { get; set; }

        /// <summary>
        /// Gets or sets the member filter.
        /// When specified, only information about the named member is returned.
        /// </summary>
        /// <value>
        /// The member identifier to filter by, or null to retrieve all members.
        /// </value>
        /// <remarks>
        /// Member identifiers can be in various formats:
        /// - SIP channel: "SIP/1001"
        /// - IAX channel: "IAX2/1001"
        /// - Local channel: "Local/1001@from-internal"
        /// - Agent: "Agent/1001"
        /// 
        /// When combined with Queue filter, returns information only about
        /// the specified member in the specified queue.
        /// </remarks>
        public string? Member { get; set; }

        /// <summary>
        /// Returns the event type that indicates completion of the QueueStatus action.
        /// </summary>
        /// <returns>The Type of QueueStatusCompleteEvent</returns>
        /// <remarks>
        /// The QueueStatusCompleteEvent is sent by Asterisk to indicate that all
        /// queue status information has been transmitted. This event marks the
        /// end of the response sequence for this action.
        /// 
        /// Sequence example:
        /// 1. Multiple QueueParams events
        /// 2. Multiple QueueMember events  
        /// 3. Multiple QueueEntry events
        /// 4. Single QueueStatusComplete event (completion marker)
        /// </remarks>
        public override Type ActionCompleteEventClass()
        {
            return typeof(QueueStatusCompleteEvent);
        }
    }
}