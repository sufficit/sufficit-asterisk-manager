using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The QueueRemoveAction removes a member from a call queue.
    /// This action is essential for dynamic call center management and agent logout functionality.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: QueueRemove
    /// Purpose: Remove a member (agent) from a call queue
    /// Privilege Required: agent,all
    /// Implementation: apps/app_queue.c
    /// Available since: Asterisk 1.2
    /// 
    /// Required Parameters:
    /// - Queue: The name of the queue (Required)
    /// - Interface: The interface/channel to remove (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Member Removal:
    /// - Removes member from queue immediately
    /// - Member stops receiving new calls
    /// - Does not affect active calls in progress
    /// - Changes are persistent if queue configured for it
    /// 
    /// Interface Matching:
    /// - Must match exactly the interface used when adding
    /// - Case-sensitive matching
    /// - Includes any state interface specifications
    /// - Wildcards not supported
    /// 
    /// Usage Scenarios:
    /// - Agent logout/end of shift
    /// - Dynamic queue membership management
    /// - Emergency queue evacuation
    /// - Skill-based routing changes
    /// - Temporary member suspension
    /// - Queue restructuring
    /// - System maintenance procedures
    /// 
    /// Queue Management Integration:
    /// - Works with QueueAddAction for dynamic management
    /// - Combine with QueuePauseAction for temporary removal
    /// - Use QueueStatusAction to verify removal
    /// - Coordinate with shift management systems
    /// 
    /// Call Handling:
    /// - Active calls continue until completion
    /// - New calls not routed to removed member
    /// - Queue strategy recalculates without member
    /// - Statistics updated to reflect removal
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.2
    /// - Consistent behavior across versions
    /// - Enhanced member tracking in newer versions
    /// - Compatible with all queue strategies
    /// 
    /// Implementation Notes:
    /// This action is implemented in apps/app_queue.c in Asterisk source code.
    /// Removal is immediate and affects queue statistics.
    /// Member must exist in queue for successful removal.
    /// 
    /// Error Conditions:
    /// - Queue does not exist
    /// - Interface not found in queue
    /// - Invalid interface specification
    /// - Insufficient privileges
    /// - Queue configuration errors
    /// 
    /// Best Practices:
    /// - Verify member exists before removal
    /// - Log member removals for audit trails
    /// - Coordinate with call center management systems
    /// - Consider graceful removal during low traffic
    /// - Update external monitoring systems
    /// 
    /// Security Considerations:
    /// - Requires appropriate manager privileges
    /// - Can affect call center operations
    /// - Should be restricted to authorized personnel
    /// - Monitor for unauthorized removals
    /// 
    /// Example Usage:
    /// <code>
    /// // Basic member removal
    /// var remove = new QueueRemoveAction("support", "SIP/1001");
    /// 
    /// // Remove with action ID for tracking
    /// var remove = new QueueRemoveAction("support", "SIP/1001");
    /// remove.ActionId = "remove_001";
    /// </code>
    /// </remarks>
    /// <seealso cref="QueueAddAction"/>
    /// <seealso cref="QueuePauseAction"/>
    /// <seealso cref="QueueStatusAction"/>
    public class QueueRemoveAction : ManagerAction
    {
        /// <summary>
        /// Creates a new empty QueueRemoveAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set both Queue and Interface
        /// properties before sending the action.
        /// </remarks>
        public QueueRemoveAction()
        {
        }

        /// <summary>
        /// Creates a new QueueRemoveAction that removes the specified member from the queue.
        /// </summary>
        /// <param name="queue">The name of the queue (Required)</param>
        /// <param name="iface">The interface to remove (Required)</param>
        /// <remarks>
        /// Queue Requirements:
        /// - Must be an existing queue
        /// - Case-sensitive name matching
        /// - Queue must allow member management
        /// 
        /// Interface Requirements:
        /// - Must exactly match the interface in the queue
        /// - Case-sensitive matching
        /// - Include any state interface specifications
        /// - Format: "Technology/Resource" (e.g., "SIP/1001")
        /// 
        /// Common Interface Examples:
        /// - "SIP/1001" (SIP endpoint)
        /// - "PJSIP/agent1" (PJSIP endpoint)
        /// - "IAX2/1001" (IAX2 peer)
        /// - "DAHDI/1" (DAHDI channel)
        /// - "Local/1001@from-queue" (Local channel)
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when queue or iface is null</exception>
        /// <exception cref="ArgumentException">Thrown when queue or iface is empty</exception>
        public QueueRemoveAction(string queue, string iface)
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
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "QueueRemove"</value>
        public override string Action => "QueueRemove";

        /// <summary>
        /// Gets or sets the name of the queue from which the member will be removed.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The queue name.
        /// </value>
        /// <remarks>
        /// Queue Identification:
        /// - Must match existing queue configuration
        /// - Case-sensitive name matching
        /// - Queue name as defined in queues.conf or realtime
        /// 
        /// Queue Types Supported:
        /// - Static queues (defined in queues.conf)
        /// - Dynamic queues (created via manager interface)
        /// - Realtime queues (stored in database)
        /// - Mixed configuration queues
        /// 
        /// Queue Name Examples:
        /// - "support" (customer support queue)
        /// - "sales" (sales team queue)
        /// - "tier1_support" (first level support)
        /// - "callbacks" (callback queue)
        /// - "emergency" (emergency response queue)
        /// 
        /// Queue Validation:
        /// - Queue must exist at removal time
        /// - Queue must be accessible with current privileges
        /// - Queue configuration must allow member changes
        /// - Queue must not be in a locked state
        /// 
        /// Error Scenarios:
        /// - Queue name not found
        /// - Insufficient privileges for queue
        /// - Queue configuration prevents member changes
        /// - Queue temporarily unavailable
        /// </remarks>
        public string? Queue { get; set; }

        /// <summary>
        /// Gets or sets the interface to remove from the queue.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The channel interface specification.
        /// </value>
        /// <remarks>
        /// Interface Matching Requirements:
        /// 
        /// Exact Match Required:
        /// - Must match exactly as added to queue
        /// - Case-sensitive string comparison
        /// - Include all original parameters
        /// - No wildcard or partial matching
        /// 
        /// Interface Format Examples:
        /// 
        /// SIP Interfaces:
        /// - "SIP/1001" (basic SIP peer)
        /// - "SIP/agent1" (named SIP peer)
        /// - Must match sip.conf configuration
        /// 
        /// PJSIP Interfaces:
        /// - "PJSIP/1001" (PJSIP endpoint)
        /// - "PJSIP/agent1" (named endpoint)
        /// - Must match pjsip.conf configuration
        /// 
        /// IAX2 Interfaces:
        /// - "IAX2/1001" (IAX2 peer)
        /// - Must match iax.conf configuration
        /// 
        /// DAHDI Interfaces:
        /// - "DAHDI/1" (DAHDI channel 1)
        /// - "DAHDI/g1" (DAHDI group 1)
        /// - Must have hardware configured
        /// 
        /// Local Interfaces:
        /// - "Local/1001@from-queue" (local channel)
        /// - Context must exist in dialplan
        /// - Useful for complex routing scenarios
        /// 
        /// State Interface Considerations:
        /// - If member was added with StateInterface, remove using original Interface
        /// - StateInterface parameter is not used in removal
        /// - Removal affects both Interface and StateInterface monitoring
        /// 
        /// Common Mistakes:
        /// - Using different case than original addition
        /// - Omitting context in Local channels
        /// - Using display name instead of interface name
        /// - Including extra parameters not in original
        /// 
        /// Verification Steps:
        /// 1. Check current queue members with QueueStatusAction
        /// 2. Use exact interface string from queue status
        /// 3. Verify removal with another QueueStatusAction
        /// 4. Monitor queue events for confirmation
        /// 
        /// Troubleshooting:
        /// - "Interface not found": Check exact spelling and case
        /// - "Permission denied": Verify queue management privileges
        /// - "Queue not found": Confirm queue name exists
        /// - "Operation failed": Check queue configuration and member state
        /// </remarks>
        public string? Interface { get; set; }
    }
}