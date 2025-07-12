using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The QueueAddAction adds a new member to a call queue.
    /// This action is essential for dynamic call center management.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: QueueAdd
    /// Purpose: Add a member (agent) to a call queue
    /// Privilege Required: agent,all
    /// Implementation: apps/app_queue.c
    /// Available since: Asterisk 1.2
    /// 
    /// Required Parameters:
    /// - Queue: The name of the queue (Required)
    /// - Interface: The interface/channel to add (Required)
    /// 
    /// Optional Parameters:
    /// - MemberName: Display name for the member (Optional)
    /// - Penalty: Priority penalty (Optional, default: 0)
    /// - Paused: Whether member starts paused (Optional, default: false)
    /// - StateInterface: Alternative interface for state monitoring (Optional)
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Member Types:
    /// - Agent Channels: Agent/1001
    /// - SIP Endpoints: SIP/1001
    /// - IAX2 Endpoints: IAX2/1001
    /// - DAHDI Channels: DAHDI/1
    /// - Local Channels: Local/1001@from-queue
    /// - PJSIP Endpoints: PJSIP/1001
    /// 
    /// Penalty System:
    /// - 0: Highest priority (calls offered first)
    /// - 1-9: Lower priority tiers
    /// - Higher numbers = lower priority
    /// - Members with same penalty called in round-robin fashion
    /// 
    /// State Monitoring:
    /// - Interface state determines member availability
    /// - StateInterface allows monitoring different channel
    /// - Useful for hotdesking and shared lines
    /// - Supports device state subscriptions
    /// 
    /// Usage Scenarios:
    /// - Dynamic agent login/logout
    /// - Shift-based queue management
    /// - Skill-based routing setup
    /// - Temporary queue assignments
    /// - Supervisor queue additions
    /// - Emergency staffing adjustments
    /// 
    /// Call Distribution:
    /// - Members called based on queue strategy
    /// - Penalty affects call order
    /// - Paused members receive no calls
    /// - State monitoring prevents calls to unavailable members
    /// 
    /// Queue Strategies:
    /// - ringall: Ring all available members
    /// - roundrobin: Ring members in rotation
    /// - leastrecent: Ring member who answered longest ago
    /// - fewestcalls: Ring member with fewest completed calls
    /// - random: Ring random available member
    /// - rrmemory: Round robin with memory
    /// 
    /// Asterisk Versions:
    /// - 1.2+: Basic functionality
    /// - 1.6+: Enhanced member management
    /// - 11+: Improved state interface support
    /// - 12+: StateInterface parameter added
    /// - 13+: Additional member properties
    /// 
    /// Implementation Notes:
    /// This action is implemented in apps/app_queue.c in Asterisk source code.
    /// Changes take effect immediately for new calls.
    /// Member addition is persistent across Asterisk restarts if configured.
    /// 
    /// Error Conditions:
    /// - Queue does not exist
    /// - Interface already in queue
    /// - Invalid interface specification
    /// - Insufficient privileges
    /// - Invalid penalty value
    /// 
    /// Example Usage:
    /// <code>
    /// // Basic agent addition
    /// var add = new QueueAddAction("support", "SIP/1001");
    /// 
    /// // Agent with display name
    /// var add = new QueueAddAction("support", "SIP/1001", "John Doe");
    /// 
    /// // Agent with penalty (lower priority)
    /// var add = new QueueAddAction("support", "SIP/1001", "John Doe", 2);
    /// </code>
    /// </remarks>
    /// <seealso cref="QueueRemoveAction"/>
    /// <seealso cref="QueuePauseAction"/>
    /// <seealso cref="QueueStatusAction"/>
    public class QueueAddAction : ManagerAction
    {
        /// <summary>
        /// Creates a new empty QueueAddAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set at least the Queue and Interface
        /// properties before sending the action. Other properties are optional.
        /// </remarks>
        public QueueAddAction()
        {
        }

        /// <summary>
        /// Creates a new QueueAddAction that adds a member to the specified queue.
        /// </summary>
        /// <param name="queue">The name of the queue (Required)</param>
        /// <param name="iface">The interface to add (Required)</param>
        /// <remarks>
        /// Queue Naming:
        /// - Must match existing queue configuration
        /// - Case-sensitive
        /// - Usually alphanumeric with underscores
        /// - Examples: "support", "sales", "tier1_support"
        /// 
        /// Interface Examples:
        /// - "SIP/1001" (SIP endpoint)
        /// - "IAX2/1001" (IAX2 endpoint)
        /// - "DAHDI/1" (DAHDI channel)
        /// - "Local/1001@from-queue" (Local channel)
        /// - "PJSIP/1001" (PJSIP endpoint)
        /// - "Agent/1001" (Agent channel - if using)
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when queue or iface is null</exception>
        /// <exception cref="ArgumentException">Thrown when queue or iface is empty</exception>
        public QueueAddAction(string queue, string iface)
        {
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));
            if (string.IsNullOrWhiteSpace(queue))
                throw new ArgumentException("Queue cannot be empty", nameof(queue));
            if (iface == null)
                throw new ArgumentNullException(nameof(iface));
            if (string.IsNullOrWhiteSpace(iface))
                throw new ArgumentException("Interface cannot be empty", nameof(iface));

            Queue = queue;
            Interface = iface;
        }

        /// <summary>
        /// Creates a new QueueAddAction with queue, interface, and member name.
        /// </summary>
        /// <param name="queue">The name of the queue (Required)</param>
        /// <param name="iface">The interface to add (Required)</param>
        /// <param name="memberName">The display name for the member (Required)</param>
        /// <remarks>
        /// Member Name Usage:
        /// - Displayed in queue status and logs
        /// - Helps identify agents in reports
        /// - Can be different from interface name
        /// - Examples: "John Doe", "Agent 1001", "Support - John"
        /// - Used in call center management interfaces
        /// - Appears in queue member events
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown when any parameter is empty</exception>
        public QueueAddAction(string queue, string iface, string memberName) : this(queue, iface)
        {
            if (memberName == null)
                throw new ArgumentNullException(nameof(memberName));
            if (string.IsNullOrWhiteSpace(memberName))
                throw new ArgumentException("MemberName cannot be empty", nameof(memberName));

            MemberName = memberName;
        }

        /// <summary>
        /// Creates a new QueueAddAction with all basic parameters.
        /// </summary>
        /// <param name="queue">The name of the queue (Required)</param>
        /// <param name="iface">The interface to add (Required)</param>
        /// <param name="memberName">The display name for the member (Required)</param>
        /// <param name="penalty">The penalty for this member (Optional, default: 0)</param>
        /// <remarks>
        /// Penalty System:
        /// - 0: Highest priority (default)
        /// - 1-9: Common priority levels
        /// - Higher numbers = lower priority
        /// - Calls offered to lower penalty members first
        /// 
        /// Penalty Examples:
        /// - 0: Senior agents, supervisors
        /// - 1: Experienced agents
        /// - 2: Regular agents
        /// - 3: New/trainee agents
        /// - 4: Backup/overflow agents
        /// 
        /// Call Distribution with Penalties:
        /// - All penalty 0 members tried first
        /// - If no penalty 0 members available, try penalty 1
        /// - Continues to higher penalties as needed
        /// - Round-robin within same penalty level
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when any string parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown when any string parameter is empty</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when penalty is negative</exception>
        public QueueAddAction(string queue, string iface, string memberName, int penalty) : this(queue, iface, memberName)
        {
            if (penalty < 0)
                throw new ArgumentOutOfRangeException(nameof(penalty), "Penalty cannot be negative");

            Penalty = penalty;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "QueueAdd"</value>
        public override string Action => "QueueAdd";

        /// <summary>
        /// Gets or sets the name of the queue where the member will be added.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The queue name.
        /// </value>
        /// <remarks>
        /// Queue Configuration Requirements:
        /// - Queue must exist in queues.conf or be dynamically created
        /// - Queue name is case-sensitive
        /// - Must have appropriate permissions for member management
        /// 
        /// Queue Name Examples:
        /// - "support" (basic support queue)
        /// - "sales" (sales team queue)
        /// - "tier1_support" (first level support)
        /// - "emergency" (urgent issues)
        /// - "callbacks" (callback queue)
        /// 
        /// Queue Types:
        /// - Static: Defined in queues.conf
        /// - Dynamic: Created via realtime or manager interface
        /// - Realtime: Stored in database
        /// - Mixed: Combination of static and dynamic members
        /// 
        /// Queue must be configured with:
        /// - Strategy (ringall, roundrobin, etc.)
        /// - Timeout settings
        /// - Music on hold class
        /// - Join/leave sounds
        /// - Member management permissions
        /// </remarks>
        public string? Queue { get; set; }

        /// <summary>
        /// Gets or sets the interface to add to the queue.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The channel interface specification.
        /// </value>
        /// <remarks>
        /// Interface Format Guidelines:
        /// 
        /// SIP Interfaces:
        /// - "SIP/1001" (basic SIP peer)
        /// - "SIP/agent1" (named SIP peer)
        /// - Must be configured in sip.conf or realtime
        /// 
        /// PJSIP Interfaces:
        /// - "PJSIP/1001" (PJSIP endpoint)
        /// - "PJSIP/agent1" (named endpoint)
        /// - Must be configured in pjsip.conf or realtime
        /// 
        /// IAX2 Interfaces:
        /// - "IAX2/1001" (IAX2 peer)
        /// - Must be configured in iax.conf
        /// 
        /// DAHDI Interfaces:
        /// - "DAHDI/1" (DAHDI channel 1)
        /// - "DAHDI/g1" (DAHDI group 1)
        /// - Must have physical hardware configured
        /// 
        /// Local Interfaces:
        /// - "Local/1001@from-queue" (local channel)
        /// - Useful for complex routing scenarios
        /// - Allows dialplan integration
        /// 
        /// Agent Interfaces (deprecated):
        /// - "Agent/1001" (agent channel)
        /// - Legacy agent system
        /// - Modern systems use SIP/PJSIP interfaces
        /// 
        /// Interface Requirements:
        /// - Must be valid and configured in Asterisk
        /// - Interface should be able to receive calls
        /// - State monitoring should work for the interface
        /// - Interface should not already be in the queue
        /// </remarks>
        public string? Interface { get; set; }

        /// <summary>
        /// Gets or sets the display name for the queue member.
        /// This property is optional.
        /// </summary>
        /// <value>
        /// Human-readable name for the member (default: interface name).
        /// </value>
        /// <remarks>
        /// Member Name Benefits:
        /// - Improves queue status readability
        /// - Helps in call center reporting
        /// - Appears in manager events
        /// - Used in wallboard displays
        /// - Facilitates agent identification
        /// 
        /// Naming Conventions:
        /// - "FirstName LastName" (e.g., "John Doe")
        /// - "Department - Name" (e.g., "Support - John")
        /// - "Agent ID - Name" (e.g., "1001 - John Doe")
        /// - "Location Name" (e.g., "NYC Office - John")
        /// 
        /// Display Locations:
        /// - Queue status commands
        /// - Manager events (QueueMemberEvent)
        /// - Call center dashboards
        /// - Reporting systems
        /// - Agent performance metrics
        /// 
        /// Best Practices:
        /// - Use consistent naming format
        /// - Include enough information for identification
        /// - Avoid special characters that may cause parsing issues
        /// - Consider privacy requirements
        /// - Update when agent information changes
        /// </remarks>
        public string? MemberName { get; set; }

        /// <summary>
        /// Gets or sets the penalty for this queue member.
        /// This property is optional.
        /// </summary>
        /// <value>
        /// Priority penalty (default: 0 - highest priority).
        /// </value>
        /// <remarks>
        /// Penalty System Details:
        /// 
        /// Priority Levels:
        /// - 0: Highest priority (calls offered first)
        /// - 1-9: Decreasing priority levels
        /// - 10+: Very low priority (rarely used)
        /// 
        /// Call Distribution Logic:
        /// 1. Queue rings all available members with penalty 0
        /// 2. If no penalty 0 members answer, try penalty 1
        /// 3. Continue to higher penalties as needed
        /// 4. Within same penalty, use queue strategy (round-robin, etc.)
        /// 
        /// Common Penalty Schemes:
        /// 
        /// Simple Tiering:
        /// - 0: Senior agents/supervisors
        /// - 1: Regular agents
        /// - 2: Junior agents
        /// 
        /// Skill-Based Routing:
        /// - 0: Specialists for complex issues
        /// - 1: General agents
        /// - 2: Trainee agents
        /// 
        /// Geographic Routing:
        /// - 0: Local agents
        /// - 1: Regional agents
        /// - 2: Remote agents
        /// 
        /// Dynamic Penalty Management:
        /// - Use QueuePenaltyAction to change penalties
        /// - Implement time-based penalty changes
        /// - Adjust based on queue load
        /// - Consider agent performance metrics
        /// 
        /// Performance Considerations:
        /// - Lower penalties increase call volume
        /// - Higher penalties provide backup coverage
        /// - Balance workload across penalty levels
        /// - Monitor queue statistics for optimization
        /// </remarks>
        public int Penalty { get; set; }

        /// <summary>
        /// Gets or sets whether the queue member should be paused when added.
        /// This property is optional.
        /// </summary>
        /// <value>
        /// True if member should start paused, false for active (default: false).
        /// </value>
        /// <remarks>
        /// Paused State Behavior:
        /// - Paused members do not receive calls
        /// - Member remains in queue but inactive
        /// - Useful for break time, training, or after-call work
        /// - Can be unpaused later with QueuePauseAction
        /// 
        /// Use Cases for Starting Paused:
        /// - Agent is in training mode
        /// - Agent is on break/lunch
        /// - Agent doing administrative work
        /// - Testing queue configuration
        /// - Gradual agent activation
        /// 
        /// Pause Management:
        /// - Use QueuePauseAction to change pause state
        /// - Monitor pause reasons for reporting
        /// - Implement automatic unpause policies
        /// - Track pause time for productivity metrics
        /// 
        /// Queue Status with Paused Members:
        /// - Paused members shown in queue status
        /// - Marked as unavailable for calls
        /// - Still count toward total member count
        /// - State changes generate events
        /// </remarks>
        public bool Paused { get; set; }

        /// <summary>
        /// Gets or sets an alternate interface for device state monitoring.
        /// This property is optional and available since Asterisk 12.
        /// </summary>
        /// <value>
        /// Alternative interface for state monitoring (default: same as Interface).
        /// </value>
        /// <remarks>
        /// State Interface Purpose:
        /// - Monitor different device for availability
        /// - Useful for hotdesking scenarios
        /// - Supports shared line appearances
        /// - Enables complex routing scenarios
        /// 
        /// Use Cases:
        /// 
        /// Hotdesking:
        /// - Interface: "SIP/desk1" (physical phone)
        /// - StateInterface: "SIP/agent1" (agent's mobile)
        /// - Calls ring desk phone but monitor agent's mobile status
        /// 
        /// Shared Lines:
        /// - Interface: "SIP/1001" (primary line)
        /// - StateInterface: "SIP/1001&SIP/1002" (monitor both)
        /// - Multiple devices share same queue member
        /// 
        /// Mobile Integration:
        /// - Interface: "Local/1001@queue-callback" (callback channel)
        /// - StateInterface: "SIP/1001" (agent's SIP device)
        /// - Monitor SIP state but use callback for delivery
        /// 
        /// Custom State Providers:
        /// - Interface: "SIP/1001" (SIP phone)
        /// - StateInterface: "Custom:agent1" (custom device state)
        /// - Use external state information
        /// 
        /// State Monitoring:
        /// - Device states: UNKNOWN, NOT_INUSE, INUSE, BUSY, INVALID, UNAVAILABLE, RINGING, RINGINUSE, ONHOLD
        /// - INUSE/BUSY: Member unavailable for calls
        /// - NOT_INUSE: Member available for calls
        /// - UNAVAILABLE: Member completely offline
        /// 
        /// Configuration Requirements:
        /// - StateInterface must be valid device
        /// - Device state subscriptions must work
        /// - May require additional Asterisk modules
        /// - Custom states need custom providers
        /// </remarks>
        public string? StateInterface { get; set; }
    }
}