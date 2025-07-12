using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The MailboxStatusAction checks if a mailbox contains waiting messages.
    /// This action is essential for voicemail integration and unified communications.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: MailboxStatus
    /// Purpose: Check voicemail mailbox status and waiting message count
    /// Privilege Required: call,reporting,all
    /// Implementation: app_voicemail.c
    /// Available since: Asterisk 1.0
    /// 
    /// Required Parameters:
    /// - Mailbox: The mailbox to check (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Mailbox Format:
    /// - "1001": Mailbox number (uses default context)
    /// - "1001@default": Mailbox with context
    /// - "1001,1002": Multiple mailboxes (comma-separated)
    /// - "1001@ctx1,1002@ctx2": Mixed contexts
    /// 
    /// Response Information:
    /// - Mailbox: The queried mailbox name
    /// - Waiting: Number of waiting messages
    /// - Context: Voicemail context
    /// - Status: Message waiting status
    /// 
    /// Usage Scenarios:
    /// - Message waiting indicator (MWI) implementation
    /// - Unified communications dashboards
    /// - Phone system status displays
    /// - Mobile app voicemail counters
    /// - Email notification triggers
    /// - Call center agent notifications
    /// - Reception desk information
    /// - Softphone presence integration
    /// 
    /// Voicemail Integration:
    /// - Works with app_voicemail module
    /// - Supports traditional voicemail systems
    /// - Integrates with external voicemail
    /// - Compatible with IMAP voicemail storage
    /// - Works with ODBC voicemail storage
    /// 
    /// Message Waiting Indicator (MWI):
    /// - Drives phone LED indicators
    /// - Email notification systems
    /// - SMS alert integration
    /// - Push notification services
    /// - Dashboard visual indicators
    /// 
    /// Multi-mailbox Support:
    /// - Check multiple mailboxes simultaneously
    /// - Supports different contexts per mailbox
    /// - Efficient batch checking
    /// - Useful for shared mailboxes
    /// - Department-wide status checking
    /// 
    /// Real-time Applications:
    /// - Live dashboard updates
    /// - Mobile app synchronization
    /// - Desktop softphone integration
    /// - Web-based voicemail portals
    /// - Call center wallboards
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.0
    /// - Enhanced context support in later versions
    /// - Multi-mailbox support added over time
    /// - Improved IMAP integration in modern versions
    /// 
    /// Implementation Notes:
    /// This action is implemented in app_voicemail.c in Asterisk source code.
    /// Requires voicemail module to be loaded and configured.
    /// Response includes detailed message count information.
    /// Works with both file-based and database voicemail storage.
    /// 
    /// Voicemail Configuration:
    /// - Requires voicemail.conf configuration
    /// - Mailbox must exist in configuration
    /// - Context must be defined
    /// - Storage method affects response details
    /// 
    /// Performance Considerations:
    /// - Lightweight operation for single mailbox
    /// - Multiple mailboxes may increase response time
    /// - Database storage generally faster than file
    /// - IMAP storage may have higher latency
    /// 
    /// Security Considerations:
    /// - Requires appropriate manager privileges
    /// - Mailbox information may be sensitive
    /// - Consider access control for mailbox queries
    /// - Audit mailbox status requests if needed
    /// 
    /// Example Usage:
    /// <code>
    /// // Single mailbox check
    /// var mailboxStatus = new MailboxStatusAction("1001@default");
    /// 
    /// // Multiple mailboxes
    /// var multiStatus = new MailboxStatusAction("1001,1002,1003");
    /// 
    /// // Mixed contexts
    /// var mixedStatus = new MailboxStatusAction("1001@users,2000@managers");
    /// </code>
    /// </remarks>
    /// <seealso cref="MailboxCountAction"/>
    public class MailboxStatusAction : ManagerAction
    {
        /// <summary>
        /// Creates a new empty MailboxStatusAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set the Mailbox property
        /// before sending the action.
        /// </remarks>
        public MailboxStatusAction()
        {
        }

        /// <summary>
        /// Creates a new MailboxStatusAction for the specified mailbox.
        /// </summary>
        /// <param name="mailbox">The mailbox to check (Required)</param>
        /// <remarks>
        /// Mailbox Format Options:
        /// 
        /// Single Mailbox:
        /// - "1001": Uses default context
        /// - "1001@users": Specific context
        /// - "sales": Named mailbox
        /// 
        /// Multiple Mailboxes:
        /// - "1001,1002,1003": Multiple in default context
        /// - "1001@users,1002@users": Multiple in same context
        /// - "1001@users,2000@managers": Multiple with different contexts
        /// 
        /// Context Handling:
        /// - If no context specified, "default" is used
        /// - Context must exist in voicemail.conf
        /// - Different contexts can have different storage methods
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when mailbox is null</exception>
        /// <exception cref="ArgumentException">Thrown when mailbox is empty</exception>
        public MailboxStatusAction(string mailbox)
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
        /// <value>Always returns "MailboxStatus"</value>
        public override string Action => "MailboxStatus";

        /// <summary>
        /// Gets or sets the mailbox(es) to query for status.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The mailbox specification.
        /// </value>
        /// <remarks>
        /// Mailbox Specification Formats:
        /// 
        /// Single Mailbox Formats:
        /// - "1001": Mailbox number in default context
        /// - "1001@default": Mailbox with explicit default context
        /// - "1001@users": Mailbox in specific context
        /// - "sales@department": Named mailbox in context
        /// - "operator": Named mailbox in default context
        /// 
        /// Multiple Mailbox Formats:
        /// - "1001,1002,1003": Multiple mailboxes in default context
        /// - "1001@users,1002@users,1003@users": Multiple in same context
        /// - "1001@users,2000@managers,3000@support": Mixed contexts
        /// - "sales@dept,support@dept": Named mailboxes in context
        /// 
        /// Context Requirements:
        /// - Context must exist in voicemail.conf
        /// - If no context specified, "default" is assumed
        /// - Context determines voicemail storage location
        /// - Different contexts can use different storage methods
        /// 
        /// Mailbox Configuration:
        /// voicemail.conf example:
        /// <code>
        /// [default]
        /// 1001 => 1234,John Doe,john@company.com
        /// 1002 => 5678,Jane Smith,jane@company.com
        /// 
        /// [users]
        /// 1001 => 1234,User One,user1@company.com
        /// 1002 => 5678,User Two,user2@company.com
        /// 
        /// [managers]
        /// 2000 => 9999,Manager One,mgr1@company.com
        /// </code>
        /// 
        /// Multi-mailbox Benefits:
        /// - Efficient batch status checking
        /// - Reduced manager action overhead
        /// - Useful for department-wide checks
        /// - Supports shared mailbox monitoring
        /// - Enables bulk status updates
        /// 
        /// Response Behavior:
        /// - Single mailbox: Status for specified mailbox
        /// - Multiple mailboxes: Combined status (any waiting = true)
        /// - Non-existent mailbox: Usually returns no waiting messages
        /// - Invalid context: May return error or no results
        /// 
        /// Storage Method Support:
        /// - File-based storage: Standard voicemail files
        /// - ODBC storage: Database-backed voicemail
        /// - IMAP storage: Email server integration
        /// - Hybrid storage: Mixed storage methods
        /// 
        /// Naming Conventions:
        /// - Numeric: Traditional phone numbers (1001, 2000)
        /// - Named: Descriptive names (sales, support, manager)
        /// - Departmental: Department codes (dept01, team_a)
        /// - Functional: Role-based (operator, receptionist)
        /// 
        /// Special Considerations:
        /// - Case-sensitive mailbox names
        /// - Context separation for multi-tenant systems
        /// - International character support
        /// - Length limitations vary by storage method
        /// 
        /// Error Handling:
        /// - Invalid mailbox format may cause errors
        /// - Non-existent context typically returns empty
        /// - Malformed multi-mailbox strings may fail
        /// - Storage system errors affect results
        /// </remarks>
        public string? Mailbox { get; set; }
    }
}