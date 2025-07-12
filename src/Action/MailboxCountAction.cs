using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The MailboxCountAction queries detailed message counts for a specific voicemail mailbox.
    /// This action provides comprehensive information about new, old, urgent, and total messages.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: MailboxCount
    /// Purpose: Get detailed message counts for voicemail mailbox
    /// Privilege Required: call,reporting,all
    /// Implementation: app_voicemail.c
    /// Available since: Asterisk 1.0
    /// 
    /// Required Parameters:
    /// - Mailbox: The mailbox to query (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Detailed Count Information:
    /// - NewMessages: Number of unread/new messages
    /// - OldMessages: Number of read/old messages
    /// - UrgentMessages: Number of urgent messages (if supported)
    /// - TotalMessages: Total message count
    /// 
    /// Mailbox Format:
    /// - "1001": Mailbox number (uses default context)
    /// - "1001@default": Mailbox with context
    /// - "1001@users": Mailbox in specific context
    /// 
    /// Usage Scenarios:
    /// - Detailed voicemail reporting
    /// - Mailbox usage analytics
    /// - Storage capacity planning
    /// - User activity monitoring
    /// - Billing and usage tracking
    /// - Compliance and audit trails
    /// - Customer service insights
    /// - System resource planning
    /// 
    /// Reporting Applications:
    /// - Administrative dashboards
    /// - Usage statistics reports
    /// - Capacity planning tools
    /// - User behavior analysis
    /// - Storage optimization
    /// - Cost analysis reports
    /// 
    /// Unified Communications Integration:
    /// - Advanced message indicators
    /// - Detailed status displays
    /// - Analytics dashboards
    /// - Mobile app detailed views
    /// - Web portal statistics
    /// - Email notification details
    /// 
    /// Difference from MailboxStatus:
    /// - MailboxStatus: Simple waiting/not-waiting status
    /// - MailboxCount: Detailed breakdown of message counts
    /// - MailboxCount: More comprehensive information
    /// - MailboxCount: Better for analytics and reporting
    /// 
    /// Message Categories:
    /// 
    /// New Messages:
    /// - Unread messages
    /// - Recently received
    /// - Trigger MWI indicators
    /// - Primary user notification
    /// 
    /// Old Messages:
    /// - Previously read messages
    /// - Archived messages
    /// - Retained for reference
    /// - Storage consumption tracking
    /// 
    /// Urgent Messages:
    /// - High priority messages
    /// - Caller-marked urgent
    /// - System-flagged priority
    /// - Special notification handling
    /// 
    /// Storage Management:
    /// - Monitor mailbox usage
    /// - Plan storage capacity
    /// - Implement cleanup policies
    /// - Track usage trends
    /// - Optimize storage allocation
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.0
    /// - Enhanced message categorization in newer versions
    /// - Improved storage reporting
    /// - Better integration with external systems
    /// 
    /// Implementation Notes:
    /// This action is implemented in app_voicemail.c in Asterisk source code.
    /// Requires voicemail module to be loaded and configured.
    /// Response includes detailed count breakdown.
    /// Works with all voicemail storage methods.
    /// 
    /// Voicemail Storage Support:
    /// - File-based storage: Direct file counting
    /// - ODBC storage: Database query results
    /// - IMAP storage: Email server statistics
    /// - Hybrid storage: Combined counting methods
    /// 
    /// Performance Considerations:
    /// - Single mailbox query is lightweight
    /// - File-based storage may require directory scanning
    /// - Database storage typically faster
    /// - IMAP storage may have higher latency
    /// 
    /// Security Considerations:
    /// - Requires appropriate manager privileges
    /// - Mailbox information may be sensitive
    /// - Consider access control for detailed counts
    /// - Audit detailed count access if needed
    /// 
    /// Example Usage:
    /// <code>
    /// // Get detailed count for specific mailbox
    /// var mailboxCount = new MailboxCountAction("1001@default");
    /// 
    /// // Query with action ID for tracking
    /// var trackedCount = new MailboxCountAction("1001@users");
    /// trackedCount.ActionId = "count_001";
    /// 
    /// // Administrative mailbox analysis
    /// var adminCount = new MailboxCountAction("admin@management");
    /// </code>
    /// </remarks>
    /// <seealso cref="MailboxStatusAction"/>
    public class MailboxCountAction : ManagerAction
    {
        /// <summary>
        /// Creates a new empty MailboxCountAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set the Mailbox property
        /// before sending the action.
        /// </remarks>
        public MailboxCountAction()
        {
        }

        /// <summary>
        /// Creates a new MailboxCountAction for the specified mailbox.
        /// </summary>
        /// <param name="mailbox">The mailbox to query for detailed counts (Required)</param>
        /// <remarks>
        /// Mailbox Format Requirements:
        /// 
        /// Single Mailbox Format:
        /// - "1001": Uses default context
        /// - "1001@users": Specific context
        /// - "admin@management": Named mailbox in context
        /// 
        /// Context Handling:
        /// - If no context specified, "default" is used
        /// - Context must exist in voicemail.conf
        /// - Context determines storage location and method
        /// 
        /// Count Details Returned:
        /// - New/unread message count
        /// - Old/read message count
        /// - Urgent message count (if supported)
        /// - Total message count
        /// - Storage usage information
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when mailbox is null</exception>
        /// <exception cref="ArgumentException">Thrown when mailbox is empty</exception>
        public MailboxCountAction(string mailbox)
        {
            if (mailbox == null)
                throw new ArgumentNullException(nameof(mailbox));
            if (string.IsNullOrWhiteSpace(mailbox))
                throw new ArgumentException("Mailbox cannot be empty", nameof(mailbox));

            Mailbox = mailbox;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "MailboxCount"</value>
        public override string Action => "MailboxCount";

        /// <summary>
        /// Gets or sets the mailbox to query for detailed message counts.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The mailbox specification.
        /// </value>
        /// <remarks>
        /// Mailbox Specification Format:
        /// 
        /// Basic Format:
        /// - "1001": Mailbox number in default context
        /// - "2000": Another mailbox in default context
        /// - "operator": Named mailbox in default context
        /// 
        /// Context-Specific Format:
        /// - "1001@default": Explicit default context
        /// - "1001@users": User context mailbox
        /// - "admin@management": Management context
        /// - "support@department": Department-specific mailbox
        /// 
        /// Mailbox Configuration Requirements:
        /// 
        /// voicemail.conf Configuration:
        /// <code>
        /// [default]
        /// 1001 => 1234,John Doe,john@company.com
        /// 1002 => 5678,Jane Smith,jane@company.com
        /// 
        /// [users]
        /// 1001 => 1234,User One,user1@company.com
        /// 1002 => 5678,User Two,user2@company.com
        /// 
        /// [management]
        /// admin => 9999,Administrator,admin@company.com
        /// </code>
        /// 
        /// Count Response Details:
        /// 
        /// Message Categories:
        /// - NewMessages: Unread/new messages requiring attention
        /// - OldMessages: Previously read/archived messages
        /// - UrgentMessages: High priority messages (caller-marked)
        /// - TotalMessages: Sum of all message categories
        /// 
        /// Storage Information:
        /// - Disk usage for file-based storage
        /// - Database records for ODBC storage
        /// - Email count for IMAP storage
        /// - Quota utilization if configured
        /// 
        /// Usage Analytics:
        /// 
        /// User Behavior Analysis:
        /// - Message reading patterns
        /// - Archive vs delete preferences
        /// - Urgent message frequency
        /// - Mailbox utilization trends
        /// 
        /// System Planning:
        /// - Storage capacity requirements
        /// - User training needs
        /// - Policy enforcement effectiveness
        /// - Resource allocation optimization
        /// 
        /// Business Intelligence:
        /// - Department communication patterns
        /// - Customer callback requirements
        /// - Peak message volume periods
        /// - Service level implications
        /// 
        /// Administrative Applications:
        /// 
        /// Mailbox Management:
        /// - Identify unused mailboxes
        /// - Monitor storage consumption
        /// - Plan cleanup activities
        /// - Optimize retention policies
        /// 
        /// User Support:
        /// - Assist with mailbox organization
        /// - Provide usage statistics
        /// - Identify training opportunities
        /// - Support productivity improvements
        /// 
        /// Compliance and Auditing:
        /// - Message retention compliance
        /// - Storage policy enforcement
        /// - Usage pattern analysis
        /// - Regulatory requirement fulfillment
        /// 
        /// Error Handling:
        /// - Invalid mailbox format: Parse error
        /// - Non-existent mailbox: Zero counts returned
        /// - Context not found: Default context attempted
        /// - Storage system error: Error response
        /// 
        /// Best Practices:
        /// - Verify mailbox exists before querying
        /// - Handle empty/zero count responses
        /// - Cache counts for dashboard applications
        /// - Monitor count trends over time
        /// - Coordinate with storage management
        /// </remarks>
        public string? Mailbox { get; set; }
    }
}