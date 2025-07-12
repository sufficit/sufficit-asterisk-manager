using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The QueuePenaltyAction sets the penalty for a queue member.
    /// This action enables dynamic priority management for call distribution in call centers.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: QueuePenalty
    /// Purpose: Change member penalty (priority) in queue(s)
    /// Privilege Required: agent,all
    /// Implementation: apps/app_queue.c
    /// Available since: Asterisk 1.4
    /// 
    /// Required Parameters:
    /// - Interface: The interface/channel to modify (Required)
    /// - Penalty: The new penalty value (Required)
    /// 
    /// Optional Parameters:
    /// - Queue: Specific queue to modify (Optional - all queues if not specified)
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Penalty System:
    /// - Lower penalty = Higher priority
    /// - 0 = Highest priority (calls offered first)
    /// - 1-9 = Common priority levels
    /// - Higher numbers = Lower priority
    /// - Same penalty = Round-robin distribution
    /// 
    /// Call Distribution Logic:
    /// 1. All penalty 0 members tried first
    /// 2. If no penalty 0 members available, try penalty 1
    /// 3. Continue to higher penalties as needed
    /// 4. Round-robin within same penalty level
    /// 5. Queue strategy applies within penalty groups
    /// 
    /// Usage Scenarios:
    /// - Skill-based routing adjustments
    /// - Performance-based prioritization
    /// - Shift-based priority changes
    /// - Training agent management
    /// - Emergency priority escalation
    /// - Load balancing optimization
    /// - Service level management
    /// - Agent development tracking
    /// 
    /// Dynamic Priority Management:
    /// - Adjust priorities based on performance
    /// - Respond to call volume changes
    /// - Implement time-based priorities
    /// - Manage training and experienced agents
    /// - Handle emergency situations
    /// - Optimize service levels
    /// 
    /// Call Center Applications:
    /// - Performance-based routing
    /// - Agent skill development
    /// - Workload distribution
    /// - Service level optimization
    /// - Training program management
    /// - Emergency response procedures
    /// 
    /// Business Benefits:
    /// - Improved customer experience
    /// - Better resource utilization
    /// - Enhanced service levels
    /// - Effective agent development
    /// - Flexible workforce management
    /// - Optimized call handling
    /// 
    /// Penalty Strategies:
    /// 
    /// Skill-Based:
    /// - 0: Subject matter experts
    /// - 1: Experienced agents
    /// - 2: General agents
    /// - 3: New agents
    /// 
    /// Performance-Based:
    /// - 0: Top performers
    /// - 1: Above average performers
    /// - 2: Average performers
    /// - 3: Developing agents
    /// 
    /// Availability-Based:
    /// - 0: Full-time available
    /// - 1: Part-time available
    /// - 2: Backup agents
    /// - 3: Overflow coverage
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.4
    /// - Enhanced penalty management in newer versions
    /// - Improved queue statistics with penalties
    /// - Better integration with reporting systems
    /// 
    /// Implementation Notes:
    /// This action is implemented in apps/app_queue.c in Asterisk source code.
    /// Changes take effect immediately for new calls.
    /// Penalty changes are persistent until changed again.
    /// Member must exist in queue(s) for successful penalty change.
    /// 
    /// Error Conditions:
    /// - Interface not found in any queue
    /// - Invalid penalty value (negative)
    /// - Queue does not exist (when specified)
    /// - Interface not member of specified queue
    /// - Insufficient privileges
    /// 
    /// Best Practices:
    /// - Document penalty schemes for consistency
    /// - Monitor penalty distribution effects
    /// - Regular penalty reviews and adjustments
    /// - Balance workload across penalty levels
    /// - Train staff on penalty impact
    /// 
    /// Performance Impact:
    /// - Immediate effect on call distribution
    /// - May affect queue statistics
    /// - Influences service level metrics
    /// - Changes agent workload patterns
    /// - Impacts customer wait times
    /// 
    /// Example Usage:
    /// <code>
    /// // Set highest priority for expert agent
    /// var penalty = new QueuePenaltyAction("SIP/expert01", "0");
    /// 
    /// // Lower priority for training agent in specific queue
    /// var trainingPenalty = new QueuePenaltyAction("SIP/trainee01", "3", "support");
    /// 
    /// // Adjust priority for performance improvement
    /// var adjustment = new QueuePenaltyAction("SIP/agent123", "1");
    /// </code>
    /// </remarks>
    /// <seealso cref="QueueAddAction"/>
    /// <seealso cref="QueueRemoveAction"/>
    /// <seealso cref="QueuePauseAction"/>
    /// <seealso cref="QueueStatusAction"/>
    public class QueuePenaltyAction : ManagerAction
    {
        /// <summary>
        /// Creates a new empty QueuePenaltyAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set Interface and Penalty
        /// properties before sending the action. Queue is optional.
        /// </remarks>
        public QueuePenaltyAction()
        {
        }

        /// <summary>
        /// Creates a new QueuePenaltyAction for all queues.
        /// </summary>
        /// <param name="iface">The interface to modify penalty for (Required)</param>
        /// <param name="penalty">The new penalty value (Required)</param>
        /// <remarks>
        /// Global Penalty Change:
        /// - Affects the member in ALL queues they belong to
        /// - Most common penalty operation
        /// - Maintains consistent priority across queues
        /// - Simplifies workforce management
        /// 
        /// Interface Requirements:
        /// - Must be valid queue member interface
        /// - Format: "Technology/Resource" (e.g., "SIP/1001")
        /// - Must exist in at least one queue
        /// 
        /// Penalty Requirements:
        /// - Must be non-negative integer
        /// - 0 = highest priority
        /// - Higher numbers = lower priority
        /// - Common range: 0-9
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when iface or penalty is null</exception>
        /// <exception cref="ArgumentException">Thrown when iface or penalty is empty</exception>
        public QueuePenaltyAction(string iface, string penalty)
        {
            if (iface == null)
                throw new ArgumentNullException(nameof(iface));
            if (string.IsNullOrWhiteSpace(iface))
                throw new ArgumentException("Interface cannot be empty", nameof(iface));
            if (penalty == null)
                throw new ArgumentNullException(nameof(penalty));
            if (string.IsNullOrWhiteSpace(penalty))
                throw new ArgumentException("Penalty cannot be empty", nameof(penalty));

            Interface = iface;
            Penalty = penalty;
        }

        /// <summary>
        /// Creates a new QueuePenaltyAction for a specific queue.
        /// </summary>
        /// <param name="iface">The interface to modify penalty for (Required)</param>
        /// <param name="penalty">The new penalty value (Required)</param>
        /// <param name="queue">The specific queue to modify (Required)</param>
        /// <remarks>
        /// Selective Penalty Change:
        /// - Affects the member in specified queue only
        /// - Other queue memberships maintain current penalties
        /// - Useful for skill-based routing scenarios
        /// - Allows granular priority control
        /// 
        /// Use Cases:
        /// - Different skills for different queues
        /// - Queue-specific training levels
        /// - Specialized expertise assignments
        /// - Department-specific priorities
        /// 
        /// Queue Requirements:
        /// - Must be existing queue
        /// - Member must belong to specified queue
        /// - Case-sensitive queue name matching
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown when any parameter is empty</exception>
        public QueuePenaltyAction(string iface, string penalty, string queue) : this(iface, penalty)
        {
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));
            if (string.IsNullOrWhiteSpace(queue))
                throw new ArgumentException("Queue cannot be empty", nameof(queue));

            Queue = queue;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "QueuePenalty"</value>
        public override string Action => "QueuePenalty";

        /// <summary>
        /// Gets or sets the interface to modify penalty for.
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
        /// - For specific queue penalty, must be member of that queue
        /// - Interface must be properly configured and accessible
        /// 
        /// Interface Discovery:
        /// - Use QueueStatusAction to list current members
        /// - Check queue configuration for member lists
        /// - Verify interface exists and is accessible
        /// - Ensure proper queue membership
        /// 
        /// Common Interface Types:
        /// 
        /// SIP/PJSIP Endpoints:
        /// - Most common in modern systems
        /// - Full feature support
        /// - Dynamic registration support
        /// - Device state monitoring
        /// 
        /// IAX2 Endpoints:
        /// - Legacy but still supported
        /// - Trunk and peer configurations
        /// - Good for remote agents
        /// 
        /// DAHDI Channels:
        /// - Hardware-based channels
        /// - Limited to available hardware
        /// - Primarily for PSTN connectivity
        /// 
        /// Local Channels:
        /// - Special routing scenarios
        /// - Complex dialplan integration
        /// - Callback implementations
        /// 
        /// Agent Channels (deprecated):
        /// - Legacy agent system
        /// - Being replaced by SIP/PJSIP
        /// - Limited modern support
        /// 
        /// Validation Requirements:
        /// - Interface name must match queue member configuration
        /// - Case-sensitive exact matching required
        /// - Must exist as active queue member
        /// - Verify membership before penalty changes
        /// 
        /// Error Scenarios:
        /// - Interface not found: Member doesn't exist
        /// - Not in queue: Member not in specified queue
        /// - Invalid format: Malformed interface name
        /// - Permission denied: Insufficient privileges
        /// </remarks>
        public string? Interface { get; set; }

        /// <summary>
        /// Gets or sets the new penalty value for the member.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The penalty value as string (must be non-negative integer).
        /// </value>
        /// <remarks>
        /// Penalty Value Guidelines:
        /// 
        /// Penalty Range:
        /// - "0": Highest priority (first choice)
        /// - "1": High priority (second choice)
        /// - "2": Medium priority (third choice)
        /// - "3": Lower priority (fourth choice)
        /// - "4-9": Progressively lower priorities
        /// - "10+": Very low priority (rarely used)
        /// 
        /// Priority Distribution Examples:
        /// 
        /// Skill-Based Distribution:
        /// - "0": Subject matter experts
        /// - "1": Senior agents
        /// - "2": Experienced agents
        /// - "3": Regular agents
        /// - "4": New agents
        /// 
        /// Performance-Based Distribution:
        /// - "0": Top performers (>95% satisfaction)
        /// - "1": High performers (90-95% satisfaction)
        /// - "2": Good performers (85-90% satisfaction)
        /// - "3": Average performers (80-85% satisfaction)
        /// - "4": Developing agents (<80% satisfaction)
        /// 
        /// Training-Based Distribution:
        /// - "0": Fully certified agents
        /// - "1": Advanced training completed
        /// - "2": Intermediate training completed
        /// - "3": Basic training completed
        /// - "4": In training/probationary
        /// 
        /// Availability-Based Distribution:
        /// - "0": Full-time, dedicated agents
        /// - "1": Full-time, multi-queue agents
        /// - "2": Part-time agents
        /// - "3": Backup/overflow agents
        /// - "4": Emergency/on-call agents
        /// 
        /// Call Distribution Impact:
        /// 
        /// Queue Strategy Integration:
        /// - Penalties processed before queue strategy
        /// - Round-robin applies within penalty groups
        /// - Least recent applies within penalty groups
        /// - Fewest calls applies within penalty groups
        /// 
        /// Service Level Effects:
        /// - Lower penalties improve answer times
        /// - Higher penalties provide backup coverage
        /// - Balanced distribution optimizes resources
        /// - Proper penalty design improves KPIs
        /// 
        /// Business Considerations:
        /// 
        /// Customer Experience:
        /// - Route to best available agents first
        /// - Ensure expertise matches call complexity
        /// - Minimize customer wait times
        /// - Maintain consistent service quality
        /// 
        /// Agent Development:
        /// - Use penalties to manage training progression
        /// - Reward performance improvements
        /// - Provide development pathways
        /// - Balance workload for learning
        /// 
        /// Operational Efficiency:
        /// - Optimize resource utilization
        /// - Reduce call handling times
        /// - Minimize repeat contacts
        /// - Improve first call resolution
        /// 
        /// Dynamic Penalty Management:
        /// 
        /// Time-Based Changes:
        /// - Different penalties for different shifts
        /// - Peak time priority adjustments
        /// - After-hours coverage optimization
        /// 
        /// Performance-Based Changes:
        /// - Reward improvements with lower penalties
        /// - Provide development support with appropriate penalties
        /// - Adjust based on quality scores
        /// 
        /// Skill-Based Changes:
        /// - Adjust as agents gain expertise
        /// - Specialize in certain call types
        /// - Cross-training progression
        /// 
        /// Validation Requirements:
        /// - Must be non-negative integer string
        /// - No decimal points or special characters
        /// - Reasonable range (0-99 typically)
        /// - Consistent with organizational penalty scheme
        /// </remarks>
        public string? Penalty { get; set; }

        /// <summary>
        /// Gets or sets the queue to modify penalty in.
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
        /// - Member remains at current penalty in other queues
        /// - Allows queue-specific skill management
        /// - Useful for specialized routing
        /// 
        /// All Queues (Queue = null or empty):
        /// - Affects ALL queues the member belongs to
        /// - Global penalty change operation
        /// - Maintains consistent priority across system
        /// - Simplifies management for general agents
        /// 
        /// Queue Selection Guidelines:
        /// 
        /// Use Specific Queue When:
        /// - Agent has different skill levels per queue
        /// - Queue-specific training or certification
        /// - Department-specific performance levels
        /// - Specialized knowledge requirements
        /// - Different service level targets
        /// 
        /// Use All Queues When:
        /// - Agent skill level is consistent across queues
        /// - General performance improvement/decline
        /// - System-wide priority changes
        /// - Simplified management requirements
        /// - Uniform training progression
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
        /// - "spanish_support" (language-specific queue)
        /// 
        /// Multi-Queue Scenarios:
        /// 
        /// Skill-Based Example:
        /// - Agent skilled in "general_support" (penalty 2)
        /// - Agent expert in "billing" (penalty 0)
        /// - Agent learning "technical" (penalty 4)
        /// 
        /// Performance-Based Example:
        /// - High performer in all queues (penalty 0)
        /// - Struggling with specific queue type (penalty 3)
        /// - Improving performance gradually (penalty 2?1?0)
        /// 
        /// Training-Based Example:
        /// - Certified for "basic_support" (penalty 1)
        /// - Training for "advanced_support" (penalty 3)
        /// - Mentoring "new_agent_queue" (penalty 0)
        /// 
        /// Error Handling:
        /// - Invalid queue name: Action may fail
        /// - Member not in queue: No effect or error
        /// - Queue not accessible: Permission error
        /// - Configuration error: System error response
        /// </remarks>
        public string? Queue { get; set; }
    }
}