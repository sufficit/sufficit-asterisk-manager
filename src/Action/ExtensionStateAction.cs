using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The ExtensionStateAction queries the current state of an extension in a specific context.
    /// This action provides real-time extension availability and status information.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: ExtensionState
    /// Purpose: Query the current state of a specific extension
    /// Privilege Required: call,reporting,all
    /// Available since: Asterisk 1.2
    /// 
    /// Required Parameters:
    /// - Exten: The extension to query (Required)
    /// - Context: The context containing the extension (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Extension States:
    /// - NOT_INUSE (0): Extension is available and not in use
    /// - INUSE (1): Extension is currently in use
    /// - BUSY (2): Extension is busy
    /// - UNAVAILABLE (3): Extension is unavailable
    /// - RINGING (4): Extension is ringing
    /// - RINGINUSE (5): Extension is ringing and in use
    /// - ONHOLD (6): Extension is on hold
    /// 
    /// State Determination:
    /// - Based on device state providers
    /// - Considers all devices for the extension
    /// - Aggregates states for extensions with multiple devices
    /// - Updates in real-time as devices change state
    /// 
    /// Device State Sources:
    /// - SIP/PJSIP endpoints
    /// - IAX2 peers
    /// - DAHDI channels
    /// - Custom device state providers
    /// - Park slots
    /// - Conference rooms
    /// 
    /// Usage Scenarios:
    /// - Call center presence monitoring
    /// - Busy lamp field (BLF) implementation
    /// - Auto-attendant routing decisions
    /// - Call forwarding logic
    /// - Extension availability checking
    /// - Real-time dashboard displays
    /// - Call queue agent status
    /// 
    /// Presence Applications:
    /// - Phone system status lights
    /// - Softphone presence indicators
    /// - Call center wallboards
    /// - Mobile app status displays
    /// - Web-based phone directories
    /// 
    /// Integration Notes:
    /// - Works with hint priorities in dialplan
    /// - Requires proper device state subscriptions
    /// - May need DEVICE_STATE() function in dialplan
    /// - Compatible with custom device state providers
    /// 
    /// Dialplan Hints:
    /// - Extensions must have hint priorities defined
    /// - Hints map extensions to device states
    /// - Multiple devices can be monitored per extension
    /// - Custom state logic supported via dialplan
    /// 
    /// Real-time Updates:
    /// - Use ExtensionStatus events for real-time monitoring
    /// - Subscribe to extension state changes
    /// - Implement event-driven presence systems
    /// - Cache states for performance optimization
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.2
    /// - Enhanced state reporting in newer versions
    /// - Additional states added over time
    /// - Improved device state providers
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// State information is retrieved from device state subsystem.
    /// Response includes numeric state and text description.
    /// 
    /// Performance Considerations:
    /// - Lightweight operation with minimal overhead
    /// - State cached by Asterisk for quick responses
    /// - Suitable for frequent polling if needed
    /// - Consider event subscriptions for real-time updates
    /// 
    /// Example Usage:
    /// <code>
    /// // Check extension state
    /// var state = new ExtensionStateAction("1001", "from-internal");
    /// 
    /// // Check with action ID
    /// var state = new ExtensionStateAction("1001", "from-internal");
    /// state.ActionId = "state_001";
    /// </code>
    /// </remarks>
    /// <seealso cref="StatusAction"/>
    /// <seealso cref="CoreShowChannelsAction"/>
    public class ExtensionStateAction : ManagerAction
    {
        /// <summary>
        /// Creates a new empty ExtensionStateAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set both Exten and Context
        /// properties before sending the action.
        /// </remarks>
        public ExtensionStateAction()
        {
        }

        /// <summary>
        /// Creates a new ExtensionStateAction for the specified extension and context.
        /// </summary>
        /// <param name="exten">The extension to query (Required)</param>
        /// <param name="context">The context containing the extension (Required)</param>
        /// <remarks>
        /// Extension Requirements:
        /// - Must exist in the specified context
        /// - Should have hint priority defined for accurate state
        /// - Can be literal extension or pattern match result
        /// 
        /// Context Requirements:
        /// - Must be valid dialplan context
        /// - Context must be accessible from manager interface
        /// - Case-sensitive context name matching
        /// 
        /// Extension Examples:
        /// - "1001" (literal extension number)
        /// - "sales" (named extension)
        /// - "*72" (feature code extension)
        /// - "s" (start extension in context)
        /// 
        /// Context Examples:
        /// - "from-internal" (internal extensions)
        /// - "default" (default context)
        /// - "users" (user extensions context)
        /// - "queues" (queue extensions context)
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when exten or context is null</exception>
        /// <exception cref="ArgumentException">Thrown when exten or context is empty</exception>
        public ExtensionStateAction(string exten, string context)
        {
            if (exten == null)
                throw new ArgumentNullException(nameof(exten));
            if (string.IsNullOrWhiteSpace(exten))
                throw new ArgumentException("Extension cannot be empty", nameof(exten));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(context))
                throw new ArgumentException("Context cannot be empty", nameof(context));

            Exten = exten;
            Context = context;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "ExtensionState"</value>
        public override string Action => "ExtensionState";

        /// <summary>
        /// Gets or sets the extension to query.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The extension number or name to check.
        /// </value>
        /// <remarks>
        /// Extension Specification:
        /// 
        /// Format Types:
        /// - Numeric: "1001", "2000", "5551234567"
        /// - Named: "john", "reception", "conference"
        /// - Feature codes: "*72", "#123", "*8"
        /// - Special: "s", "h", "i", "t" (dialplan specials)
        /// 
        /// Extension Resolution:
        /// - Exact match preferred
        /// - Pattern matching may apply
        /// - Case-sensitive for named extensions
        /// - Must exist in specified context
        /// 
        /// Hint Requirements:
        /// For accurate state reporting, extensions should have hints defined:
        /// <code>
        /// exten => 1001,hint,SIP/1001
        /// exten => 2000,hint,SIP/sales&SIP/manager
        /// exten => *8,hint,Park:701@parkedcalls
        /// </code>
        /// 
        /// State Sources:
        /// - Device states from hint definitions
        /// - Multiple devices aggregated per extension
        /// - Custom device state providers
        /// - Park slot states
        /// - Conference room states
        /// 
        /// Extension Types:
        /// 
        /// User Extensions:
        /// - Individual user phone numbers
        /// - Direct device mappings
        /// - Personal extension assignments
        /// 
        /// Service Extensions:
        /// - Reception, operator
        /// - Department numbers
        /// - Shared service lines
        /// 
        /// Feature Extensions:
        /// - Call parking (*8)
        /// - Call forwarding (*72)
        /// - Conference rooms
        /// - Voicemail access
        /// 
        /// Queue Extensions:
        /// - Call queue entry points
        /// - Department queues
        /// - Support levels
        /// 
        /// Virtual Extensions:
        /// - Ring groups
        /// - Hunt groups
        /// - Follow-me configurations
        /// 
        /// Validation:
        /// - Extension must exist in context
        /// - Hint definition recommended
        /// - Device mappings should be valid
        /// - State providers must be configured
        /// </remarks>
        public string? Exten { get; set; }

        /// <summary>
        /// Gets or sets the context that contains the extension to query.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The dialplan context name.
        /// </value>
        /// <remarks>
        /// Context Requirements:
        /// 
        /// Context Definition:
        /// - Must be defined in dialplan (extensions.conf)
        /// - Case-sensitive name matching
        /// - Must be accessible from manager interface
        /// - Should contain the specified extension
        /// 
        /// Common Context Names:
        /// 
        /// User Contexts:
        /// - "from-internal" (internal user extensions)
        /// - "users" (user extension context)
        /// - "employees" (employee extensions)
        /// - "default" (default context)
        /// 
        /// Service Contexts:
        /// - "from-trunk" (incoming calls)
        /// - "outbound-routing" (outgoing calls)
        /// - "queues" (call queue contexts)
        /// - "conferences" (conference rooms)
        /// 
        /// Feature Contexts:
        /// - "parkedcalls" (call parking)
        /// - "features" (feature codes)
        /// - "voicemail" (voicemail access)
        /// - "app-*" (FreePBX application contexts)
        /// 
        /// Context Structure:
        /// <code>
        /// [from-internal]
        /// exten => 1001,1,Dial(SIP/1001,20)
        /// exten => 1001,hint,SIP/1001
        /// 
        /// exten => 1002,1,Dial(SIP/1002,20)
        /// exten => 1002,hint,SIP/1002
        /// </code>
        /// 
        /// Context Permissions:
        /// - Manager interface must have access
        /// - Context should not be restricted
        /// - Verify context exists before querying
        /// - Check dialplan for context definition
        /// 
        /// Context Validation:
        /// - Use "dialplan show context" in CLI
        /// - Verify extension exists in context
        /// - Check for proper hint definitions
        /// - Ensure device state providers are working
        /// 
        /// Context Hierarchy:
        /// - Includes may affect extension resolution
        /// - Pattern matches considered
        /// - Priority levels important for hints
        /// - Global variables may influence behavior
        /// 
        /// Troubleshooting:
        /// - "Context not found": Verify context exists
        /// - "Extension not found": Check extension definition
        /// - "No state": Verify hint definitions
        /// - "Invalid context": Check permissions and spelling
        /// </remarks>
        public string? Context { get; set; }
    }
}