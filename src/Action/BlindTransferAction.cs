using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The BlindTransferAction redirects all channels currently bridged to the specified channel 
    /// to a new destination without consultation. This implements blind/unsupervised transfers.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: BlindTransfer
    /// Purpose: Perform blind (unsupervised) call transfer
    /// Privilege Required: call,all
    /// Implementation: res/res_manager_devicestate.c and channel core
    /// Available since: Asterisk 13+ (enhanced in 16+)
    /// 
    /// Required Parameters:
    /// - Channel: The channel to transfer from (Required)
    /// - Context: Destination dialplan context (Required)
    /// - Exten: Destination extension (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Transfer Types:
    /// - Blind Transfer: No consultation, immediate transfer
    /// - vs Attended Transfer: Consultation before completion
    /// - vs Redirect: Lower-level channel redirection
    /// 
    /// Transfer Behavior:
    /// - Immediately transfers bridged channels to destination
    /// - No consultation or announcement
    /// - Transferring party is disconnected
    /// - Target channels continue to destination
    /// - Transfer cannot be undone once initiated
    /// 
    /// Channel Requirements:
    /// - Channel must be actively bridged
    /// - Channel must support transfer operations
    /// - Destination context and extension must exist
    /// - Sufficient privileges required
    /// 
    /// Usage Scenarios:
    /// - Reception desk call transfers
    /// - IVR system transfers
    /// - Call center queue transfers
    /// - Automated call routing
    /// - Emergency call redirections
    /// - Call distribution systems
    /// - Customer service workflows
    /// - PBX attendant functions
    /// 
    /// Call Center Applications:
    /// - Agent-initiated transfers
    /// - Supervisor transfers
    /// - Queue overflow transfers
    /// - Skill-based routing transfers
    /// - Department transfers
    /// - Escalation procedures
    /// 
    /// Business Applications:
    /// - Reception transfers to extensions
    /// - Department routing
    /// - After-hours transfers to voicemail
    /// - Emergency transfers to on-call
    /// - Customer service escalations
    /// 
    /// Transfer Flow:
    /// 1. Identify bridged channels
    /// 2. Validate destination exists
    /// 3. Initiate transfer process
    /// 4. Disconnect transferring channel
    /// 5. Connect remaining channels to destination
    /// 6. Generate transfer events
    /// 
    /// Asterisk Versions:
    /// - 13+: Basic BlindTransfer support
    /// - 16+: Enhanced transfer framework
    /// - 18+: Improved error handling and events
    /// - Modern: Full transfer feature support
    /// 
    /// Implementation Notes:
    /// This action uses Asterisk's transfer framework.
    /// Transfer success depends on channel technology support.
    /// Some channel types may not support all transfer features.
    /// Events are generated during transfer process.
    /// 
    /// Error Conditions:
    /// - Channel not found or not active
    /// - Channel not in bridged state
    /// - Destination context/extension invalid
    /// - Transfer not supported by channel technology
    /// - Insufficient privileges
    /// - Call already in transfer state
    /// 
    /// Event Generation:
    /// - BlindTransfer events during process
    /// - Channel state change events
    /// - Bridge events for channel movements
    /// - Transfer success/failure notifications
    /// 
    /// Security Considerations:
    /// - Requires appropriate manager privileges
    /// - Can redirect active calls
    /// - Should validate transfer destinations
    /// - Consider authorization for transfer targets
    /// - Monitor for unauthorized transfers
    /// 
    /// Performance Impact:
    /// - Minimal overhead for transfer operation
    /// - Brief processing during channel bridging
    /// - Network traffic for signaling updates
    /// - Logging and event generation overhead
    /// 
    /// Example Usage:
    /// <code>
    /// // Basic blind transfer
    /// var transfer = new BlindTransferAction(
    ///     "SIP/1001-00000001", 
    ///     "from-internal", 
    ///     "2000"
    /// );
    /// 
    /// // Transfer to voicemail
    /// var toVoicemail = new BlindTransferAction(
    ///     "SIP/1001-00000001", 
    ///     "voicemail", 
    ///     "1002"
    /// );
    /// 
    /// // Transfer to queue
    /// var toQueue = new BlindTransferAction(
    ///     "SIP/1001-00000001", 
    ///     "queue-context", 
    ///     "support"
    /// );
    /// </code>
    /// </remarks>
    /// <seealso cref="RedirectAction"/>
    /// <seealso cref="AtxferAction"/>
    /// <seealso cref="BridgeAction"/>
    public class BlindTransferAction : ManagerAction
    {
        /// <summary>
        /// Creates a new empty BlindTransferAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set Channel, Context, and Exten
        /// properties before sending the action.
        /// </remarks>
        public BlindTransferAction()
        {
        }

        /// <summary>
        /// Creates a new BlindTransferAction with specified parameters.
        /// </summary>
        /// <param name="channel">The channel to transfer from (Required)</param>
        /// <param name="context">Destination dialplan context (Required)</param>
        /// <param name="extension">Destination extension (Required)</param>
        /// <remarks>
        /// Parameter Requirements:
        /// - Channel must be active and bridged
        /// - Context must exist in dialplan
        /// - Extension must exist in specified context
        /// 
        /// Transfer Process:
        /// 1. Validates channel is in transferable state
        /// 2. Checks destination context and extension exist
        /// 3. Initiates blind transfer to destination
        /// 4. Disconnects transferring channel
        /// 5. Connects remaining parties to destination
        /// 
        /// Common Transfer Scenarios:
        /// - Reception to extension: context="from-internal", extension="1001"
        /// - To voicemail: context="voicemail", extension="1001"
        /// - To queue: context="queues", extension="support"
        /// - To IVR: context="ivr-main", extension="s"
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown when any parameter is empty</exception>
        public BlindTransferAction(string channel, string context, string extension)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentException("Channel cannot be empty", nameof(channel));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(context))
                throw new ArgumentException("Context cannot be empty", nameof(context));
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));
            if (string.IsNullOrWhiteSpace(extension))
                throw new ArgumentException("Extension cannot be empty", nameof(extension));

            Channel = channel;
            Context = context;
            Exten = extension;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "BlindTransfer"</value>
        public override string Action => "BlindTransfer";

        /// <summary>
        /// Gets or sets the channel to transfer from.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The channel name to transfer.
        /// </value>
        /// <remarks>
        /// Channel Requirements:
        /// 
        /// Channel State Requirements:
        /// - Must be an active, connected channel
        /// - Must be in a bridged state with other channels
        /// - Must support transfer operations
        /// - Should be the channel initiating the transfer
        /// 
        /// Channel Format Examples:
        /// - "SIP/1001-00000001" (SIP channel)
        /// - "PJSIP/1001-00000001" (PJSIP channel)
        /// - "IAX2/provider-00000001" (IAX2 channel)
        /// - "DAHDI/1-1" (DAHDI channel)
        /// - "Local/1001@from-internal-00000001;1" (Local channel)
        /// 
        /// Transfer Eligibility:
        /// - Channel must be answered (Up state)
        /// - Must be bridged to other channels
        /// - Transfer features must be enabled
        /// - Channel technology must support transfers
        /// 
        /// Channel Discovery:
        /// - Use CoreShowChannelsAction to list active channels
        /// - Monitor bridge events for bridged channels
        /// - Track channels from originate actions
        /// - Verify channel state before transfer
        /// 
        /// Common Transfer Sources:
        /// - Reception desk phones
        /// - IVR system channels
        /// - Queue agent channels
        /// - Conference bridge channels
        /// - Operator console channels
        /// 
        /// Transfer Limitations:
        /// - Some channel technologies may not support transfers
        /// - Certain call states may prevent transfers
        /// - Security policies may restrict transfers
        /// - Network conditions may affect transfer success
        /// 
        /// Error Scenarios:
        /// - Channel not found: Channel has hung up
        /// - Not bridged: Channel not connected to others
        /// - Transfer denied: Technology or policy restriction
        /// - Invalid state: Channel in non-transferable state
        /// </remarks>
        public string? Channel { get; set; }

        /// <summary>
        /// Gets or sets the destination dialplan context.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The dialplan context name.
        /// </value>
        /// <remarks>
        /// Context Requirements:
        /// 
        /// Context Validation:
        /// - Must exist in extensions.conf or database
        /// - Must be accessible from current call state
        /// - Should contain the target extension
        /// - Must have proper permissions and includes
        /// 
        /// Common Transfer Contexts:
        /// 
        /// Internal Extensions:
        /// - "from-internal": Internal user extensions
        /// - "users": User extension context
        /// - "employees": Employee extensions
        /// - "departments": Department extensions
        /// 
        /// Service Contexts:
        /// - "voicemail": Voicemail access context
        /// - "queues": Call queue context
        /// - "conferences": Conference room context
        /// - "ivr-main": Main IVR context
        /// 
        /// Special Contexts:
        /// - "parkedcalls": Call parking context
        /// - "features": Feature code context
        /// - "emergency": Emergency services context
        /// - "after-hours": After hours routing
        /// 
        /// Context Design Patterns:
        /// 
        /// Hierarchical:
        /// - "company-internal": Company internal calls
        /// - "company-external": External call handling
        /// - "company-emergency": Emergency procedures
        /// 
        /// Departmental:
        /// - "sales-team": Sales department
        /// - "support-team": Support department
        /// - "management": Management team
        /// 
        /// Functional:
        /// - "incoming-calls": Incoming call routing
        /// - "outgoing-calls": Outbound call handling
        /// - "transfer-routing": Transfer destinations
        /// 
        /// Context Security:
        /// - Validate context permissions
        /// - Ensure proper includes and restrictions
        /// - Check for context-specific security policies
        /// - Monitor for unauthorized context access
        /// 
        /// Context Configuration Example:
        /// <code>
        /// [from-internal]
        /// exten => 1001,1,Dial(SIP/1001,20)
        /// exten => 1001,n,Voicemail(1001@default)
        /// 
        /// [voicemail]
        /// exten => _X.,1,VoicemailMain(${EXTEN}@default)
        /// 
        /// [queues]
        /// exten => support,1,Queue(support-queue)
        /// exten => sales,1,Queue(sales-queue)
        /// </code>
        /// </remarks>
        public string? Context { get; set; }

        /// <summary>
        /// Gets or sets the destination extension.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The dialplan extension.
        /// </value>
        /// <remarks>
        /// Extension Requirements:
        /// 
        /// Extension Validation:
        /// - Must exist in the specified context
        /// - Must have at least priority 1 defined
        /// - Should be accessible for transfers
        /// - Must be properly configured for destination
        /// 
        /// Extension Types:
        /// 
        /// Literal Extensions:
        /// - "1001": Specific extension number
        /// - "2000": Department extension
        /// - "operator": Named extension
        /// - "reception": Service extension
        /// 
        /// Special Extensions:
        /// - "s": Start extension (context entry point)
        /// - "i": Invalid extension handler
        /// - "h": Hangup extension
        /// - "t": Timeout extension
        /// 
        /// Service Extensions:
        /// - "voicemail": Voicemail access
        /// - "directory": Company directory
        /// - "operator": Operator service
        /// - "emergency": Emergency services
        /// 
        /// Queue Extensions:
        /// - "support": Support queue
        /// - "sales": Sales queue
        /// - "billing": Billing queue
        /// - "technical": Technical support
        /// 
        /// Extension Patterns:
        /// While transfers typically go to literal extensions,
        /// patterns may work in some contexts:
        /// - "_1XXX": 1000-1999 range
        /// - "_NXXXXXX": 7-digit numbers
        /// - "_[2-9]XX": 200-999 range
        /// 
        /// Transfer Destinations:
        /// 
        /// User Extensions:
        /// - Direct user phones
        /// - Department representatives
        /// - Manager extensions
        /// - Specialist extensions
        /// 
        /// Service Extensions:
        /// - Voicemail systems
        /// - Auto-attendants
        /// - Information services
        /// - Conference bridges
        /// 
        /// Queue Extensions:
        /// - Customer service queues
        /// - Technical support queues
        /// - Sales queues
        /// - Overflow queues
        /// 
        /// Extension Configuration Example:
        /// <code>
        /// ; User extension
        /// exten => 1001,1,Dial(SIP/1001,20)
        /// exten => 1001,n,Voicemail(1001@default)
        /// 
        /// ; Queue extension
        /// exten => support,1,Queue(support-queue,t)
        /// 
        /// ; Voicemail extension
        /// exten => voicemail,1,VoicemailMain()
        /// 
        /// ; Operator extension
        /// exten => operator,1,Dial(SIP/operator,10)
        /// exten => operator,n,Queue(operator-queue)
        /// </code>
        /// 
        /// Best Practices:
        /// - Verify extension exists before transfer
        /// - Use descriptive extension names
        /// - Implement proper error handling
        /// - Consider extension capacity and availability
        /// - Monitor transfer success rates
        /// </remarks>
        public string? Exten { get; set; }
    }
}