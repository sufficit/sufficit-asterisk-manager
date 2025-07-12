using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The QueuePauseAction makes a queue member temporarily unavailable or available again.
    /// This action is essential for call center agent break management and availability control.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: QueuePause
    /// Purpose: Pause or unpause a queue member (agent)
    /// Privilege Required: agent,all
    /// Implementation: apps/app_queue.c
    /// Available since: Asterisk 1.2
    /// 
    /// Required Parameters:
    /// - Interface: The interface/channel to pause/unpause (Required)
    /// - Paused: Whether to pause (true) or unpause (false) (Required)
    /// 
    /// Optional Parameters:
    /// - Queue: Specific queue to pause in (Optional - all queues if not specified)
    /// - Reason: Text description for the pause (Optional)
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Pause Behavior:
    /// - Paused members do not receive new calls
    /// - Members remain in queue but are marked unavailable
    /// - Active calls continue until completion
    /// - Changes take effect immediately
    /// - Generates QueueMemberPaused events
    /// 
    /// Pause Scope:
    /// - Single Queue: Pause member in specific queue only
    /// - All Queues: Pause member in all queues they belong to
    /// - Global vs Selective: Choose scope based on needs
    /// 
    /// Usage Scenarios:
    /// - Agent break time (lunch, coffee, etc.)
    /// - Training sessions
    /// - Administrative work
    /// - After-call work (ACW)
    /// - Personal time off
    /// - System maintenance
    /// - Emergency situations
    /// - Shift transitions
    /// 
    /// Pause Reasons:
    /// Common standardized reasons for reporting:
    /// - "Break": Regular break time
    /// - "Lunch": Lunch break
    /// - "Training": Training session
    /// - "ACW": After-call work
    /// - "Meeting": Team meeting
    /// - "Personal": Personal time
    /// - "Technical": Technical issues
    /// - "System": System maintenance
    /// 
    /// Call Center Integration:
    /// - Workforce management systems
    /// - Real-time dashboards
    /// - Performance reporting
    /// - Compliance tracking
    /// - Break scheduling
    /// - Agent productivity metrics
    /// 
    /// Event Generation:
    /// - QueueMemberPaused: When member is paused
    /// - QueueMemberUnpaused: When member is unpaused (new event names may vary)
    /// - Events include queue, interface, and reason information
    /// - Action ID correlation for tracking
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.2
    /// - Reason parameter added in later versions
    /// - Enhanced event reporting in newer versions
    /// - Consistent behavior across versions
    /// 
    /// Implementation Notes:
    /// This action is implemented in apps/app_queue.c in Asterisk source code.
    /// Changes are immediate and persistent until changed again.
    /// Member state affects queue statistics and reporting.
    /// Supports both individual queue and global queue pausing.
    /// 
    /// Queue Statistics Impact:
    /// - Paused members excluded from available count
    /// - Queue performance metrics affected
    /// - Service level calculations updated
    /// - Wait time predictions adjusted
    /// 
    /// Best Practices:
    /// - Use descriptive reason codes for reporting
    /// - Implement automatic unpause policies
    /// - Monitor pause duration for compliance
    /// - Track pause reasons for optimization
    /// - Coordinate with workforce management
    /// 
    /// Security Considerations:
    /// - Requires appropriate manager privileges
    /// - Can affect call center operations
    /// - Should be restricted to authorized users
    /// - Monitor for unauthorized pause/unpause
    /// 
    /// Example Usage:
    /// <code>
    /// // Pause agent for break in all queues
    /// var pause = new QueuePauseAction("SIP/1001", true, "Break");
    /// 
    /// // Pause agent in specific queue
    /// var pause = new QueuePauseAction("SIP/1001", "support", true, "Training");
    /// 
    /// // Unpause agent in all queues
    /// var unpause = new QueuePauseAction("SIP/1001", false);
    /// </code>
    /// </remarks>
    /// <seealso cref="QueueAddAction"/>
    /// <seealso cref="QueueRemoveAction"/>
    /// <seealso cref="QueueStatusAction"/>
    public class QueuePauseAction : ManagerAction
    {
        /// <summary>
        /// Creates a new empty QueuePauseAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set at least Interface and Paused
        /// properties before sending the action. Queue and Reason are optional.
        /// </remarks>
        public QueuePauseAction()
        {
        }

        /// <summary>
        /// Creates a new QueuePauseAction that pauses the member on all queues.
        /// </summary>
        /// <param name="iface">The interface of the member to pause (Required)</param>
        /// <remarks>
        /// Global Pause:
        /// - Pauses the member in ALL queues they belong to
        /// - Most common pause operation
        /// - Used for breaks, lunch, etc.
        /// - Affects all queue memberships simultaneously
        /// 
        /// Interface Requirements:
        /// - Must be valid queue member interface
        /// - Format: "Technology/Resource" (e.g., "SIP/1001")
        /// - Must exist in at least one queue
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when iface is null</exception>
        /// <exception cref="ArgumentException">Thrown when iface is empty</exception>
        public QueuePauseAction(string iface)
        {
            if (iface == null)
                throw new ArgumentNullException(nameof(iface));
            if (string.IsNullOrWhiteSpace(iface))
                throw new ArgumentException("Interface cannot be empty", nameof(iface));

            Interface = iface;
            Paused = true;
        }

        /// <summary>
        /// Creates a new QueuePauseAction that pauses the member in a specific queue.
        /// </summary>
        /// <param name="iface">The interface of the member to pause (Required)</param>
        /// <param name="queue">The specific queue to pause the member in (Required)</param>
        /// <remarks>
        /// Selective Pause:
        /// - Pauses member in specified queue only
        /// - Other queue memberships remain active
        /// - Useful for skill-based routing
        /// - Allows partial availability
        /// 
        /// Use Cases:
        /// - Specialist unavailable for complex issues
        /// - Training on specific queue procedures
        /// - Temporary skill unavailability
        /// - Queue-specific maintenance
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when iface or queue is null</exception>
        /// <exception cref="ArgumentException">Thrown when iface or queue is empty</exception>
        public QueuePauseAction(string iface, string queue) : this(iface)
        {
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));
            if (string.IsNullOrWhiteSpace(queue))
                throw new ArgumentException("Queue cannot be empty", nameof(queue));

            Queue = queue;
        }

        /// <summary>
        /// Creates a new QueuePauseAction that pauses or unpauses the member on all queues.
        /// </summary>
        /// <param name="iface">The interface of the member (Required)</param>
        /// <param name="paused">True to pause, false to unpause (Required)</param>
        /// <remarks>
        /// Bidirectional Control:
        /// - Can both pause and unpause members
        /// - Global scope affects all queues
        /// - Common for automated systems
        /// - Simplified interface for toggle operations
        /// 
        /// Automated Usage:
        /// - Scheduled break systems
        /// - Workforce management integration
        /// - Time-based availability control
        /// - Emergency pause/unpause procedures
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when iface is null</exception>
        /// <exception cref="ArgumentException">Thrown when iface is empty</exception>
        public QueuePauseAction(string iface, bool paused) : this(iface)
        {
            Paused = paused;
        }

        /// <summary>
        /// Creates a new QueuePauseAction with all parameters specified.
        /// </summary>
        /// <param name="iface">The interface of the member (Required)</param>
        /// <param name="queue">The queue to pause/unpause in (Required)</param>
        /// <param name="paused">True to pause, false to unpause (Required)</param>
        /// <param name="reason">The reason for the pause/unpause (Optional)</param>
        /// <remarks>
        /// Complete Control:
        /// - Full parameter specification
        /// - Selective queue targeting
        /// - Bidirectional pause/unpause
        /// - Reason tracking for reporting
        /// 
        /// Reason Tracking Benefits:
        /// - Compliance reporting
        /// - Performance analytics
        /// - Workforce optimization
        /// - Break time management
        /// - Trend analysis
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when iface or queue is null</exception>
        /// <exception cref="ArgumentException">Thrown when iface or queue is empty</exception>
        public QueuePauseAction(string iface, string queue, bool paused, string? reason = null) : this(iface, queue)
        {
            Paused = paused;
            Reason = reason;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "QueuePause"</value>
        public override string Action => "QueuePause";

        /// <summary>
        /// Gets or sets the interface to pause or unpause.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The channel interface specification.
        /// </value>
        /// <remarks>
        /// Interface Requirements:
        /// 
        /// Format Examples:
        /// - "SIP/1001" (SIP endpoint)
        /// - "PJSIP/agent1" (PJSIP endpoint)
        /// - "IAX2/1001" (IAX2 peer)
        /// - "DAHDI/1" (DAHDI channel)
        /// - "Local/1001@from-queue" (Local channel)
        /// 
        /// Membership Requirements:
        /// - Interface must be a member of at least one queue
        /// - For specific queue pause, must be member of that queue
        /// - Interface must be properly configured and accessible
        /// 
        /// State Considerations:
        /// - Interface can be in any state (busy, idle, offline)
        /// - Pause affects future call distribution only
        /// - Current calls continue until completion
        /// - Device state monitoring still functions
        /// 
        /// Technology Support:
        /// - Most channel technologies support queue membership
        /// - SIP/PJSIP: Full support with device state
        /// - IAX2: Full support with device state
        /// - DAHDI: Basic support, limited device state
        /// - Local: Support depends on underlying technology
        /// 
        /// Validation:
        /// - Interface name must match queue member configuration
        /// - Case-sensitive exact matching required
        /// - Must exist as active queue member
        /// - Verify membership before pause operations
        /// </remarks>
        public string? Interface { get; set; }

        /// <summary>
        /// Gets or sets the queue in which to pause or unpause the member.
        /// This property is optional.
        /// </summary>
        /// <value>
        /// The queue name, or null to affect all queues.
        /// </value>
        /// <remarks>
        /// Queue Scope Options:
        /// 
        /// Specific Queue (Queue specified):
        /// - Affects only the named queue
        /// - Member remains active in other queues
        /// - Allows selective availability
        /// - Useful for skill-based routing
        /// 
        /// All Queues (Queue = null or empty):
        /// - Affects ALL queues the member belongs to
        /// - Global pause/unpause operation
        /// - Most common for breaks and time off
        /// - Simplifies management for general unavailability
        /// 
        /// Queue Selection Guidelines:
        /// 
        /// Use Specific Queue When:
        /// - Member has multiple skill sets
        /// - Temporary unavailability for certain types
        /// - Training on specific queue procedures
        /// - Queue-specific technical issues
        /// - Gradual return from extended absence
        /// 
        /// Use All Queues When:
        /// - General break time (lunch, coffee, etc.)
        /// - End of shift procedures
        /// - Personal time off
        /// - System-wide maintenance
        /// - Emergency situations
        /// 
        /// Queue Name Validation:
        /// - Must match existing queue configuration
        /// - Case-sensitive exact matching
        /// - Member must belong to specified queue
        /// - Queue must be accessible with current privileges
        /// 
        /// Example Queue Names:
        /// - "support" (customer support queue)
        /// - "sales" (sales team queue)
        /// - "tier1_support" (first level support)
        /// - "billing" (billing inquiries)
        /// - "technical" (technical support)
        /// </remarks>
        public string? Queue { get; set; }

        /// <summary>
        /// Gets or sets whether to pause or unpause the interface.
        /// This property is required.
        /// </summary>
        /// <value>
        /// True to pause the member, false to unpause.
        /// </value>
        /// <remarks>
        /// Pause State Effects:
        /// 
        /// Paused (true):
        /// - Member stops receiving new calls
        /// - Remains visible in queue status as paused
        /// - Active calls continue until completion
        /// - Device state monitoring continues
        /// - Statistics reflect unavailable time
        /// - Queue distribution recalculates without member
        /// 
        /// Unpaused (false):
        /// - Member becomes available for new calls
        /// - Resumes normal queue participation
        /// - Queue distribution includes member again
        /// - Statistics reflect return to service
        /// - Previous pause reason cleared (typically)
        /// 
        /// Operational Impact:
        /// 
        /// Queue Performance:
        /// - Service levels affected by available agent count
        /// - Wait times may increase when agents paused
        /// - Distribution algorithms adjust automatically
        /// - Real-time statistics update immediately
        /// 
        /// Call Center Metrics:
        /// - Agent utilization calculations
        /// - Pause time tracking and reporting
        /// - Service level compliance monitoring
        /// - Workforce management integration
        /// 
        /// Best Practices:
        /// - Implement automatic unpause policies
        /// - Monitor excessive pause durations
        /// - Use consistent pause/unpause procedures
        /// - Coordinate with workforce management systems
        /// - Track pause reasons for optimization
        /// </remarks>
        public bool Paused { get; set; }

        /// <summary>
        /// Gets or sets the reason for pausing or unpausing the member.
        /// This property is optional.
        /// </summary>
        /// <value>
        /// Text description for the pause reason, or null.
        /// </value>
        /// <remarks>
        /// Reason Code Standards:
        /// 
        /// Common Pause Reasons:
        /// - "Break": Regular scheduled break
        /// - "Lunch": Lunch break period
        /// - "Training": Training session or meeting
        /// - "ACW": After-call work
        /// - "Personal": Personal time off
        /// - "Technical": Technical issues or support
        /// - "Meeting": Team or department meeting
        /// - "System": System maintenance or updates
        /// - "Emergency": Emergency or urgent situation
        /// - "Coaching": Coaching session with supervisor
        /// 
        /// Unpause Reasons:
        /// - "Return": Return from break
        /// - "Available": General availability
        /// - "Scheduled": Scheduled return to service
        /// - "Complete": Activity completion
        /// 
        /// Reporting Benefits:
        /// - Workforce management integration
        /// - Compliance tracking and reporting
        /// - Performance analytics and trends
        /// - Break time optimization
        /// - Agent productivity measurement
        /// - Service level impact analysis
        /// 
        /// Data Format Guidelines:
        /// - Keep reasons concise but descriptive
        /// - Use consistent terminology across organization
        /// - Consider standardized reason codes
        /// - Avoid sensitive personal information
        /// - Support internationalization if needed
        /// 
        /// Integration Considerations:
        /// - Reason appears in QueueMemberPaused events
        /// - Available in queue status reports
        /// - Used by workforce management systems
        /// - Stored in call center databases
        /// - Included in performance dashboards
        /// 
        /// Character Limits:
        /// - Keep reasons reasonably short (< 50 characters)
        /// - Avoid special characters that may cause issues
        /// - Consider display limitations in monitoring systems
        /// - Ensure compatibility with reporting tools
        /// </remarks>
        public string? Reason { get; set; }
    }
}