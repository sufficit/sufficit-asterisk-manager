using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The RedirectAction redirects a channel to a new extension in the dialplan.
    /// This action moves an active channel to a different dialplan location for continued execution.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Redirect
    /// Purpose: Move a channel to a new dialplan location
    /// Privilege Required: call,all
    /// 
    /// Required Parameters:
    /// - Channel: The channel to redirect (Required)
    /// - Context: New dialplan context (Required)
    /// - Exten: New dialplan extension (Required)
    /// - Priority: New dialplan priority (Required)
    /// 
    /// Optional Parameters:
    /// - ExtraChannel: Additional channel for dual redirect (Optional)
    /// - ExtraContext: Context for extra channel (Optional)
    /// - ExtraExten: Extension for extra channel (Optional)
    /// - ExtraPriority: Priority for extra channel (Optional)
    /// 
    /// Redirect Types:
    /// 1. Single Channel Redirect: Redirects one channel to new dialplan location
    /// 2. Dual Channel Redirect: Redirects two channels simultaneously (call transfer)
    /// 
    /// Redirect Behavior:
    /// - Channel immediately stops current dialplan execution
    /// - Jumps to specified context, extension, and priority
    /// - All variables and channel state are preserved
    /// - CDR records may be affected by the redirect
    /// - Channel continues execution from new location
    /// 
    /// Usage Scenarios:
    /// - Call transfers (attended and blind)
    /// - Call routing based on conditions
    /// - Emergency call redirection
    /// - Interactive menu navigation
    /// - Call center queue management
    /// - Custom call control applications
    /// 
    /// Dual Channel Redirect:
    /// Used for transferring bridged channels:
    /// - Both channels are redirected simultaneously
    /// - Maintains call relationship
    /// - Useful for supervised transfers
    /// - Requires ExtraChannel and related parameters
    /// 
    /// Asterisk Versions:
    /// - Available since early Asterisk versions
    /// - Dual channel redirect added in later versions
    /// - Enhanced error handling in modern versions
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// Uses Asterisk's channel control framework.
    /// May trigger various channel events during redirect.
    /// 
    /// Security Considerations:
    /// - Requires appropriate manager privileges
    /// - Can redirect calls to any dialplan location
    /// - Should validate destination context/extension
    /// - Consider authorization for redirect destinations
    /// 
    /// Error Conditions:
    /// - Channel not found or not active
    /// - Invalid context/extension/priority
    /// - Channel not in redirectable state
    /// - Insufficient privileges
    /// - Dialplan location does not exist
    /// 
    /// Example Usage:
    /// <code>
    /// // Single channel redirect
    /// var redirect = new RedirectAction(
    ///     "SIP/1001-00000001", 
    ///     "from-internal", 
    ///     "2000", 
    ///     "1"
    /// );
    /// 
    /// // Dual channel redirect (transfer)
    /// var redirect = new RedirectAction(
    ///     "SIP/1001-00000001", 
    ///     "from-internal", 
    ///     "2000", 
    ///     "1",
    ///     "SIP/1002-00000002",
    ///     "from-internal",
    ///     "2000", 
    ///     "1"
    /// );
    /// </code>
    /// </remarks>
    /// <seealso cref="BridgeAction"/>
    /// <seealso cref="HangupAction"/>
    /// <seealso cref="OriginateAction"/>
    public class RedirectAction : ManagerAction
    {
        /// <summary>
        /// Creates a new RedirectAction for a single channel.
        /// </summary>
        /// <param name="channel">The channel to redirect (Required)</param>
        /// <param name="context">New dialplan context (Required)</param>
        /// <param name="exten">New dialplan extension (Required)</param>
        /// <param name="priority">New dialplan priority (Required)</param>
        /// <remarks>
        /// Single Channel Redirect:
        /// - Redirects one channel to a new dialplan location
        /// - Most common form of redirect
        /// - Used for call routing and menu navigation
        /// 
        /// Parameter validation:
        /// - All parameters are required and cannot be null/empty
        /// - Context must exist in dialplan configuration
        /// - Extension must exist in specified context
        /// - Priority must exist for the extension
        /// </remarks>
        public RedirectAction(string channel, string context, string exten, string priority)
        {
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Exten = exten ?? throw new ArgumentNullException(nameof(exten));
            Priority = priority ?? throw new ArgumentNullException(nameof(priority));
        }

        /// <summary>
        /// Creates a new RedirectAction for dual channel redirect (transfer).
        /// </summary>
        /// <param name="channel">The first channel to redirect (Required)</param>
        /// <param name="context">New dialplan context for first channel (Required)</param>
        /// <param name="exten">New dialplan extension for first channel (Required)</param>
        /// <param name="priority">New dialplan priority for first channel (Required)</param>
        /// <param name="extraChannel">The second channel to redirect (Required for dual)</param>
        /// <param name="extraContext">New dialplan context for second channel (Required for dual)</param>
        /// <param name="extraExten">New dialplan extension for second channel (Required for dual)</param>
        /// <param name="extraPriority">New dialplan priority for second channel (Required for dual)</param>
        /// <remarks>
        /// Dual Channel Redirect:
        /// - Redirects two channels simultaneously
        /// - Used for call transfers and bridged call control
        /// - Both channels must be active and typically bridged
        /// 
        /// Transfer Scenarios:
        /// - Attended transfer: Both channels go to same destination
        /// - Complex routing: Channels go to different destinations
        /// - Call supervision: Redirect supervisor and customer
        /// </remarks>
        public RedirectAction(string channel, string context, string exten, string priority,
                            string extraChannel, string extraContext, string extraExten, string extraPriority)
            : this(channel, context, exten, priority)
        {
            ExtraChannel = extraChannel ?? throw new ArgumentNullException(nameof(extraChannel));
            ExtraContext = extraContext ?? throw new ArgumentNullException(nameof(extraContext));
            ExtraExten = extraExten ?? throw new ArgumentNullException(nameof(extraExten));
            ExtraPriority = extraPriority ?? throw new ArgumentNullException(nameof(extraPriority));
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Redirect"</value>
        public override string Action => "Redirect";

        /// <summary>
        /// Gets or sets the channel to redirect.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The channel name to redirect.
        /// </value>
        /// <remarks>
        /// Channel Requirements:
        /// - Must be a valid, active channel
        /// - Should be in a state that allows redirection
        /// - Typically channels in "Up" state work best
        /// - Format depends on channel technology
        /// 
        /// Redirectable States:
        /// - "Up": Channel is answered (most common)
        /// - "Ring": Channel is ringing (may work)
        /// - "Ringing": Outbound channel ringing (may work)
        /// 
        /// Channel Examples:
        /// - "SIP/1001-00000001" (SIP peer call)
        /// - "IAX2/provider/5551234567-00000001" (IAX trunk call)
        /// - "DAHDI/1-1" (DAHDI channel)
        /// - "Local/1001@from-internal-00000001;1" (Local channel leg)
        /// - "PJSIP/1001-00000001" (PJSIP endpoint)
        /// 
        /// The channel must exist and be in an appropriate state for redirection.
        /// Some channel states may not support redirection.
        /// </remarks>
        public string Channel { get; set; }

        /// <summary>
        /// Gets or sets the new dialplan context.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The dialplan context name.
        /// </value>
        /// <remarks>
        /// Context Requirements:
        /// - Must exist in extensions.conf or database
        /// - Must be accessible from current channel state
        /// - Case-sensitive exact match required
        /// 
        /// Common Contexts:
        /// - "default": Default context for internal calls
        /// - "from-internal": Internal user context
        /// - "from-trunk": Incoming trunk context
        /// - "outbound-routing": Outbound call routing
        /// - "ivr-menu": Interactive voice response menu
        /// - "queue": Call queue context
        /// - "voicemail": Voicemail system context
        /// 
        /// Context Access:
        /// - Some contexts may have restrictions
        /// - Check dialplan include statements
        /// - Verify context exists before redirect
        /// </remarks>
        public string Context { get; set; }

        /// <summary>
        /// Gets or sets the new dialplan extension.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The dialplan extension.
        /// </value>
        /// <remarks>
        /// Extension Types:
        /// - Literal: "1001", "2000", "911"
        /// - Special: "s" (start), "i" (invalid), "h" (hangup), "t" (timeout)
        /// - Patterns: "_1NXXNXXXXXX", "_X.", "_[2-9]XX"
        /// - Variables: Extensions with variable substitution
        /// 
        /// Extension Examples:
        /// - "1001": Specific extension number
        /// - "s": Start extension (entry point)
        /// - "2000": Another specific extension
        /// - "voicemail": Named extension
        /// - "queue": Queue extension
        /// 
        /// Extension Requirements:
        /// - Must exist in the specified context
        /// - Must have at least one priority defined
        /// - Case-sensitive for named extensions
        /// - Pattern matching follows Asterisk rules
        /// </remarks>
        public string Exten { get; set; }

        /// <summary>
        /// Gets or sets the new dialplan priority.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The dialplan priority.
        /// </value>
        /// <remarks>
        /// Priority Types:
        /// - Numeric: "1", "2", "3" (traditional)
        /// - Named: "start", "main", "end" (modern)
        /// - Relative: "n" (next), "n+101" (next plus offset)
        /// 
        /// Priority Examples:
        /// - "1": First priority (most common)
        /// - "start": Named priority label
        /// - "n": Next priority in sequence
        /// - "2": Second priority
        /// 
        /// Priority Requirements:
        /// - Must exist in the specified context/extension
        /// - Must be valid for the extension
        /// - Numeric priorities start at 1
        /// - Named priorities must be defined in dialplan
        /// 
        /// Best Practices:
        /// - Use "1" for extension entry points
        /// - Use named priorities for clarity
        /// - Avoid hardcoded high numbers
        /// - Consider using "n" for maintainability
        /// </remarks>
        public string Priority { get; set; }

        /// <summary>
        /// Gets or sets the extra channel for dual redirect.
        /// This property is optional (required for dual redirect).
        /// </summary>
        /// <value>
        /// The second channel name to redirect, or null for single redirect.
        /// </value>
        /// <remarks>
        /// Dual Redirect Usage:
        /// - Used for call transfers and complex routing
        /// - Both channels are redirected simultaneously
        /// - Typically used with bridged channels
        /// 
        /// Extra Channel Requirements:
        /// - Must be a valid, active channel when specified
        /// - Should be different from the primary channel
        /// - Often the other leg of a bridged call
        /// 
        /// Transfer Scenarios:
        /// - Attended transfer: Both channels to same destination
        /// - Consultation: Redirect consultant and customer
        /// - Conference setup: Move multiple participants
        /// 
        /// When to use:
        /// - Call transfers involving two parties
        /// - Complex call routing scenarios
        /// - Maintaining call relationships during redirect
        /// </remarks>
        public string? ExtraChannel { get; set; }

        /// <summary>
        /// Gets or sets the extra context for dual redirect.
        /// This property is optional (required when ExtraChannel is specified).
        /// </summary>
        /// <value>
        /// The dialplan context for the extra channel, or null.
        /// </value>
        /// <remarks>
        /// Extra Context Usage:
        /// - Specifies destination context for second channel
        /// - Can be same or different from primary context
        /// - Must exist in dialplan when specified
        /// 
        /// Same vs Different Contexts:
        /// - Same: Both channels to same context (common for transfers)
        /// - Different: Complex routing with different destinations
        /// 
        /// This property is required when ExtraChannel is specified.
        /// </remarks>
        public string? ExtraContext { get; set; }

        /// <summary>
        /// Gets or sets the extra extension for dual redirect.
        /// This property is optional (required when ExtraChannel is specified).
        /// </summary>
        /// <value>
        /// The dialplan extension for the extra channel, or null.
        /// </value>
        /// <remarks>
        /// Extra Extension Usage:
        /// - Specifies destination extension for second channel
        /// - Can be same or different from primary extension
        /// - Must exist in ExtraContext when specified
        /// 
        /// Transfer Examples:
        /// - Same extension: Both parties to same destination
        /// - Different extensions: Parties to different destinations
        /// 
        /// This property is required when ExtraChannel is specified.
        /// </remarks>
        public string? ExtraExten { get; set; }

        /// <summary>
        /// Gets or sets the extra priority for dual redirect.
        /// This property is optional (required when ExtraChannel is specified).
        /// </summary>
        /// <value>
        /// The dialplan priority for the extra channel, or null.
        /// </value>
        /// <remarks>
        /// Extra Priority Usage:
        /// - Specifies destination priority for second channel
        /// - Typically "1" for new extension entry point
        /// - Must exist in ExtraContext/ExtraExten when specified
        /// 
        /// This property is required when ExtraChannel is specified.
        /// Usually set to "1" to start at the beginning of the extension.
        /// </remarks>
        public string? ExtraPriority { get; set; }
    }
}