using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The SetVarAction sets the value of a channel variable or global variable.
    /// This action provides dynamic variable manipulation during call processing.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: SetVar
    /// Purpose: Set channel or global variable values
    /// Privilege Required: call,all
    /// Available since: Asterisk 1.0
    /// 
    /// Required Parameters:
    /// - Variable: The name of the variable to set (Required)
    /// - Value: The value to assign (Required)
    /// 
    /// Optional Parameters:
    /// - Channel: Channel name for channel variables (Optional - global if not specified)
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Variable Types:
    /// 1. Channel Variables: Associated with specific channel
    /// 2. Global Variables: Available system-wide
    /// 
    /// Variable Scope:
    /// - Channel Variables: Exist only for the channel lifetime
    /// - Global Variables: Persist until Asterisk restart or explicit removal
    /// - Inherited Variables: Passed to child channels
    /// 
    /// Channel Variables:
    /// - Format: Variable name without ${} syntax
    /// - Scope: Limited to specific channel
    /// - Lifetime: Channel creation to destruction
    /// - Inheritance: Can be inherited by child channels
    /// - Access: Available in dialplan as ${VARIABLE}
    /// 
    /// Global Variables:
    /// - Format: Variable name without ${} syntax
    /// - Scope: System-wide access
    /// - Lifetime: Until Asterisk restart or removal
    /// - Sharing: Available to all channels and contexts
    /// - Persistence: Not persistent across restarts
    /// 
    /// Usage Scenarios:
    /// - Call routing decisions
    /// - Customer information storage
    /// - Call center metrics
    /// - Authentication tokens
    /// - Configuration overrides
    /// - Feature flags
    /// - Debugging information
    /// - Application state management
    /// 
    /// Variable Applications:
    /// - CRM integration data
    /// - Call classification
    /// - Recording preferences
    /// - Billing information
    /// - Transfer destinations
    /// - Menu selections
    /// - Time zone settings
    /// - Language preferences
    /// 
    /// Special Variables:
    /// - CALLERID(name): Caller name
    /// - CALLERID(num): Caller number
    /// - CHANNEL(accountcode): Account code
    /// - CDR(field): CDR field values
    /// - MONITOR_EXEC: Recording commands
    /// 
    /// Variable Naming:
    /// - Case-sensitive names
    /// - Alphanumeric and underscore characters
    /// - Avoid spaces and special characters
    /// - Consistent naming conventions recommended
    /// - Descriptive names for maintainability
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.0
    /// - Enhanced variable handling in newer versions
    /// - Additional built-in variables over time
    /// - Improved inheritance mechanisms
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// Variables are immediately available after setting.
    /// Channel must exist for channel variable operations.
    /// Global variables accessible from all contexts.
    /// 
    /// Security Considerations:
    /// - Variables may contain sensitive information
    /// - Global variables accessible system-wide
    /// - Consider variable naming to avoid conflicts
    /// - Validate variable content for security
    /// 
    /// Performance Impact:
    /// - Minimal overhead for variable operations
    /// - Memory usage depends on variable content
    /// - Global variables consume system memory
    /// - Channel variables cleaned up with channel
    /// 
    /// Example Usage:
    /// <code>
    /// // Set channel variable
    /// var setVar = new SetVarAction("SIP/1001-00000001", "CUSTOMER_ID", "12345");
    /// 
    /// // Set global variable
    /// var setGlobal = new SetVarAction("SYSTEM_STATUS", "ACTIVE");
    /// 
    /// // Set special variable
    /// var setCallerID = new SetVarAction("SIP/1001-00000001", "CALLERID(name)", "John Doe");
    /// </code>
    /// </remarks>
    /// <seealso cref="GetVarAction"/>
    public class SetVarAction : ManagerAction
    {
        /// <summary>
        /// Creates a new empty SetVarAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set Variable and Value properties
        /// before sending the action. Channel is optional for global variables.
        /// </remarks>
        public SetVarAction()
        {
        }

        /// <summary>
        /// Creates a new SetVarAction that sets a global variable.
        /// </summary>
        /// <param name="variable">The name of the global variable to set (Required)</param>
        /// <param name="value">The value to assign (Required)</param>
        /// <remarks>
        /// Global Variable Usage:
        /// - Available system-wide to all channels and contexts
        /// - Persists until Asterisk restart or explicit removal
        /// - Useful for system-wide configuration and state
        /// - Can be accessed from any dialplan context
        /// 
        /// Global Variable Examples:
        /// - System status flags
        /// - Feature enable/disable switches
        /// - Emergency mode indicators
        /// - Maintenance windows
        /// - Default routing parameters
        /// 
        /// Best Practices:
        /// - Use descriptive names with consistent convention
        /// - Consider using prefixes for different subsystems
        /// - Document global variables for team knowledge
        /// - Monitor memory usage with large numbers of globals
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when variable or value is null</exception>
        /// <exception cref="ArgumentException">Thrown when variable is empty</exception>
        public SetVarAction(string variable, string value)
        {
            if (variable == null)
                throw new ArgumentNullException(nameof(variable));
            if (string.IsNullOrWhiteSpace(variable))
                throw new ArgumentException("Variable name cannot be empty", nameof(variable));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Variable = variable;
            Value = value;
        }

        /// <summary>
        /// Creates a new SetVarAction that sets a channel variable.
        /// </summary>
        /// <param name="channel">The channel name (Required)</param>
        /// <param name="variable">The name of the channel variable (Required)</param>
        /// <param name="value">The value to assign (Required)</param>
        /// <remarks>
        /// Channel Variable Usage:
        /// - Associated with specific channel only
        /// - Automatically cleaned up when channel destroyed
        /// - Available in dialplan for that channel
        /// - Can be inherited by child channels
        /// 
        /// Channel Variable Examples:
        /// - Customer identification numbers
        /// - Call classification data
        /// - Recording preferences
        /// - Transfer destinations
        /// - Authentication tokens
        /// - Call context information
        /// 
        /// Channel Requirements:
        /// - Channel must exist when action is executed
        /// - Channel name must be exact match
        /// - Works with any channel technology
        /// - Variables persist for channel lifetime
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown when channel or variable is empty</exception>
        public SetVarAction(string channel, string variable, string value) : this(variable, value)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentException("Channel cannot be empty", nameof(channel));

            Channel = channel;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "SetVar"</value>
        public override string Action => "SetVar";

        /// <summary>
        /// Gets or sets the channel name for channel variables.
        /// This property is optional (null for global variables).
        /// </summary>
        /// <value>
        /// The channel name, or null for global variables.
        /// </value>
        /// <remarks>
        /// Channel Specification:
        /// 
        /// Channel Format Examples:
        /// - "SIP/1001-00000001" (SIP channel)
        /// - "PJSIP/1001-00000001" (PJSIP channel)
        /// - "IAX2/provider-00000001" (IAX2 channel)
        /// - "DAHDI/1-1" (DAHDI channel)
        /// - "Local/1001@from-internal-00000001;1" (Local channel)
        /// 
        /// Channel vs Global:
        /// - Channel specified: Sets channel variable
        /// - Channel null/empty: Sets global variable
        /// - Channel variables scoped to specific channel
        /// - Global variables available system-wide
        /// 
        /// Channel Requirements:
        /// - Must be active, existing channel
        /// - Exact channel name matching required
        /// - Channel technology doesn't matter
        /// - Variable persists for channel lifetime
        /// 
        /// Channel Discovery:
        /// - Use CoreShowChannelsAction to list channels
        /// - Monitor channel events for active channels
        /// - Store channel names from other actions
        /// - Verify channel exists before setting variables
        /// 
        /// Variable Inheritance:
        /// - Child channels may inherit parent variables
        /// - Inheritance rules depend on channel technology
        /// - Bridging may affect variable visibility
        /// - Local channels have special inheritance rules
        /// </remarks>
        public string? Channel { get; set; }

        /// <summary>
        /// Gets or sets the name of the variable to set.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The variable name.
        /// </value>
        /// <remarks>
        /// Variable Naming Guidelines:
        /// 
        /// Naming Conventions:
        /// - Use uppercase for consistency
        /// - Underscore for word separation
        /// - Descriptive but concise names
        /// - Avoid spaces and special characters
        /// 
        /// Variable Categories:
        /// 
        /// Built-in Variables:
        /// - "CALLERID(name)": Caller ID name
        /// - "CALLERID(num)": Caller ID number
        /// - "CHANNEL(accountcode)": Account code
        /// - "CDR(userfield)": CDR user field
        /// - "MONITOR_EXEC": Recording commands
        /// 
        /// Custom Variables:
        /// - "CUSTOMER_ID": Customer identifier
        /// - "CAMPAIGN_NAME": Marketing campaign
        /// - "PRIORITY_LEVEL": Call priority
        /// - "LANGUAGE_CODE": Preferred language
        /// - "RECORDING_ENABLED": Recording preference
        /// 
        /// System Variables:
        /// - "SYSTEM_STATUS": System state
        /// - "MAINTENANCE_MODE": Maintenance flag
        /// - "EMERGENCY_ROUTE": Emergency routing
        /// - "DEBUG_LEVEL": Debug verbosity
        /// 
        /// Variable Syntax:
        /// - Do NOT include ${} brackets in variable name
        /// - Use plain variable name only
        /// - Asterisk adds ${} when accessing in dialplan
        /// - Case-sensitive variable names
        /// 
        /// Special Characters:
        /// - Parentheses: Used for function variables like CALLERID(name)
        /// - Underscores: Word separation
        /// - Numbers: Allowed in variable names
        /// - Hyphens: Generally avoided
        /// 
        /// Reserved Names:
        /// - Avoid Asterisk built-in variable names
        /// - Check dialplan documentation for conflicts
        /// - Use prefixes for application-specific variables
        /// - Coordinate naming with team conventions
        /// </remarks>
        public new string? Variable { get; set; }

        /// <summary>
        /// Gets or sets the value to assign to the variable.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The variable value.
        /// </value>
        /// <remarks>
        /// Value Specifications:
        /// 
        /// Data Types:
        /// - String: Most common type
        /// - Numeric: Integer or decimal values
        /// - Boolean: "1"/"0" or "true"/"false"
        /// - Empty: Empty string to clear variable
        /// - JSON: Structured data as JSON string
        /// 
        /// Value Examples:
        /// 
        /// String Values:
        /// - "John Doe": Person name
        /// - "Premium Customer": Classification
        /// - "en-US": Language code
        /// - "Sales Department": Department name
        /// 
        /// Numeric Values:
        /// - "12345": Customer ID
        /// - "100": Priority level
        /// - "30": Timeout seconds
        /// - "1.5": Multiplier value
        /// 
        /// Boolean Values:
        /// - "1": True/enabled
        /// - "0": False/disabled
        /// - "yes": Enabled
        /// - "no": Disabled
        /// 
        /// Special Values:
        /// - "": Empty string (clears variable)
        /// - "${EXTEN}": Reference to extension
        /// - "${CALLERID(num)}": Reference to caller ID
        /// - "Local/100@default": Channel specification
        /// 
        /// JSON Values:
        /// - '{"id":123,"name":"John"}': Customer data
        /// - '["option1","option2"]': List of choices
        /// - '{"enabled":true,"level":5}': Configuration
        /// 
        /// Value Constraints:
        /// - No specific length limits in most cases
        /// - Memory usage considerations for large values
        /// - Special characters generally allowed
        /// - Unicode support in modern Asterisk
        /// 
        /// Security Considerations:
        /// - Sanitize user input before setting variables
        /// - Avoid storing sensitive data in variables
        /// - Consider variable visibility and access
        /// - Validate value format when required
        /// 
        /// Performance Notes:
        /// - Large values consume more memory
        /// - Complex parsing may affect performance
        /// - Consider caching frequently accessed values
        /// - Clean up variables when no longer needed
        /// </remarks>
        public string? Value { get; set; }
    }
}