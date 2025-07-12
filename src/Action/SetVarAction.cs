using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The SetVarAction sets a channel variable to a specified value.
    /// This action allows dynamic modification of channel variables during call execution.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Setvar
    /// Purpose: Set channel variable values programmatically
    /// Privilege Required: call,all
    /// 
    /// Required Parameters:
    /// - Channel: The channel to set the variable on (Required)
    /// - Variable: The name of the variable to set (Required)
    /// - Value: The value to assign to the variable (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Variable Scope:
    /// - Channel variables: Available to the specific channel
    /// - Inherited variables: May be inherited by related channels
    /// - Global variables: Use Global() function syntax
    /// - Dialplan variables: Available in dialplan execution
    /// 
    /// Variable Types:
    /// - Built-in: Asterisk system variables (CHANNEL, CALLERID, etc.)
    /// - Custom: User-defined variables for application logic
    /// - Function: Variables that invoke functions (CALLERID(name))
    /// - Special: Variables with special behavior (CDR, CONNECTEDLINE)
    /// 
    /// Usage Scenarios:
    /// - Call routing based on conditions
    /// - Setting caller ID information
    /// - Configuring call features (recording, monitoring)
    /// - Application state management
    /// - CDR field customization
    /// - Call transfer preparation
    /// - Interactive voice response (IVR) logic
    /// - Call center agent assignment
    /// 
    /// Common Variables:
    /// - CALLERID(name): Caller ID name
    /// - CALLERID(num): Caller ID number
    /// - CONNECTEDLINE(name): Connected line name
    /// - CONNECTEDLINE(num): Connected line number
    /// - CDR(accountcode): Account code for billing
    /// - CDR(userfield): Custom CDR field
    /// - MONITOR_EXEC: Call recording settings
    /// - TRANSFER_CONTEXT: Transfer destination context
    /// - CHANNEL(language): Channel language
    /// - CHANNEL(musicclass): Music on hold class
    /// 
    /// Asterisk Versions:
    /// - Available since early Asterisk versions
    /// - Variable support expanded over time
    /// - Function variables added in later versions
    /// - Modern versions support complex variable types
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// Variables are stored in channel's variable list.
    /// Some variables may trigger special behavior when set.
    /// Variable changes may generate VarSet events.
    /// 
    /// Security Considerations:
    /// - Variables may contain sensitive information
    /// - Some variables affect call routing and billing
    /// - Validate variable names and values
    /// - Consider authorization for critical variables
    /// 
    /// Error Conditions:
    /// - Channel not found
    /// - Channel not in valid state
    /// - Invalid variable name
    /// - Insufficient privileges
    /// 
    /// Example Usage:
    /// <code>
    /// // Set caller ID name
    /// var setVar = new SetVarAction(
    ///     "SIP/1001-00000001", 
    ///     "CALLERID(name)", 
    ///     "John Doe"
    /// );
    /// 
    /// // Set account code for billing
    /// var setVar = new SetVarAction(
    ///     "SIP/1001-00000001", 
    ///     "CDR(accountcode)", 
    ///     "SALES_DEPT"
    /// );
    /// 
    /// // Set custom variable
    /// var setVar = new SetVarAction(
    ///     "SIP/1001-00000001", 
    ///     "CUSTOMER_ID", 
    ///     "12345"
    /// );
    /// </code>
    /// </remarks>
    /// <seealso cref="GetVarAction"/>
    /// <seealso cref="RedirectAction"/>
    public class SetVarAction : ManagerAction
    {
        /// <summary>
        /// Creates a new SetVarAction to set a channel variable.
        /// </summary>
        /// <param name="channel">The channel to set the variable on (Required)</param>
        /// <param name="variable">The name of the variable to set (Required)</param>
        /// <param name="value">The value to assign to the variable (Required)</param>
        /// <remarks>
        /// Parameter Requirements:
        /// - Channel: Must be valid, active channel
        /// - Variable: Must be valid variable name
        /// - Value: Can be any string value (including empty)
        /// 
        /// Variable naming:
        /// - Use uppercase for consistency with Asterisk
        /// - Avoid spaces and special characters except underscores
        /// - Function syntax: FUNCTION(parameter)
        /// - Built-in variables follow Asterisk conventions
        /// </remarks>
        public SetVarAction(string channel, string variable, string value)
        {
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
            Variable = variable ?? throw new ArgumentNullException(nameof(variable));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Creates a new SetVarAction with action ID for correlation.
        /// </summary>
        /// <param name="channel">The channel to set the variable on (Required)</param>
        /// <param name="variable">The name of the variable to set (Required)</param>
        /// <param name="value">The value to assign to the variable (Required)</param>
        /// <param name="actionId">Unique identifier for this action (Optional)</param>
        /// <remarks>
        /// The action ID allows correlation of responses and events with this specific request.
        /// Useful for tracking the success/failure of variable assignments.
        /// </remarks>
        public SetVarAction(string channel, string variable, string value, string actionId) 
            : this(channel, variable, value)
        {
            ActionId = actionId;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Setvar"</value>
        public override string Action => "Setvar";

        /// <summary>
        /// Gets or sets the channel to set the variable on.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The channel name where the variable will be set.
        /// </value>
        /// <remarks>
        /// Channel Requirements:
        /// - Must be a valid, active channel
        /// - Channel must be in a state that allows variable setting
        /// - Format depends on channel technology
        /// 
        /// Channel Examples:
        /// - "SIP/1001-00000001" (SIP peer call)
        /// - "IAX2/provider/5551234567-00000001" (IAX trunk call)
        /// - "DAHDI/1-1" (DAHDI channel)
        /// - "Local/1001@from-internal-00000001;1" (Local channel leg)
        /// - "PJSIP/1001-00000001" (PJSIP endpoint)
        /// 
        /// Variable Scope:
        /// - Variables are specific to this channel
        /// - Some variables may affect related channels
        /// - Variables persist for channel lifetime
        /// - Variables are available in dialplan and applications
        /// 
        /// The channel must exist and be accessible for variable operations.
        /// Use Status action or channel events to verify channel existence.
        /// </remarks>
        public string Channel { get; set; }

        /// <summary>
        /// Gets or sets the name of the variable to set.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The variable name to set.
        /// </value>
        /// <remarks>
        /// Variable Name Types:
        /// 
        /// Built-in Channel Variables:
        /// - "CHANNEL(name)": Channel name
        /// - "CHANNEL(state)": Channel state
        /// - "CHANNEL(language)": Channel language
        /// - "CHANNEL(musicclass)": Music on hold class
        /// - "CHANNEL(amaflags)": AMA flags for billing
        /// - "CHANNEL(accountcode)": Account code
        /// - "CHANNEL(peeraccount)": Peer account code
        /// - "CHANNEL(hangupsource)": Hangup source
        /// - "CHANNEL(appname)": Current application
        /// - "CHANNEL(appdata)": Application data
        /// 
        /// Caller ID Variables:
        /// - "CALLERID(name)": Caller ID name
        /// - "CALLERID(num)": Caller ID number  
        /// - "CALLERID(ani)": Automatic Number Identification
        /// - "CALLERID(ani2)": ANI II digits
        /// - "CALLERID(rdnis)": Redirecting Directory Number
        /// - "CALLERID(dnid)": Dialed Number Identification
        /// - "CALLERID(all)": Complete caller ID string
        /// 
        /// Connected Line Variables:
        /// - "CONNECTEDLINE(name)": Connected line name
        /// - "CONNECTEDLINE(num)": Connected line number
        /// - "CONNECTEDLINE(pres)": Presentation indicator
        /// - "CONNECTEDLINE(ton)": Type of number
        /// 
        /// Redirecting Variables:
        /// - "REDIRECTING(from-name)": Original caller name
        /// - "REDIRECTING(from-num)": Original caller number
        /// - "REDIRECTING(to-name)": Final destination name
        /// - "REDIRECTING(to-num)": Final destination number
        /// - "REDIRECTING(reason)": Redirection reason
        /// 
        /// CDR Variables:
        /// - "CDR(accountcode)": Account code for billing
        /// - "CDR(src)": Source number
        /// - "CDR(dst)": Destination number
        /// - "CDR(dcontext)": Destination context
        /// - "CDR(clid)": Caller ID
        /// - "CDR(channel)": Channel name
        /// - "CDR(dstchannel)": Destination channel
        /// - "CDR(lastapp)": Last application
        /// - "CDR(lastdata)": Last application data
        /// - "CDR(duration)": Call duration
        /// - "CDR(billsec)": Billable seconds
        /// - "CDR(disposition)": Call disposition
        /// - "CDR(amaflags)": AMA flags
        /// - "CDR(uniqueid)": Unique call identifier
        /// - "CDR(userfield)": User-defined field
        /// 
        /// Call Feature Variables:
        /// - "MONITOR_EXEC": Call recording execution
        /// - "MONITOR_FILENAME": Recording filename
        /// - "TRANSFER_CONTEXT": Transfer context
        /// - "PARK_TIMEOUT": Call parking timeout
        /// - "QUEUE_PRIO": Queue priority
        /// - "QUEUE_MAX_PENALTY": Maximum queue penalty
        /// 
        /// Custom Variables:
        /// - "CUSTOMER_ID": Customer identifier
        /// - "CAMPAIGN_NAME": Marketing campaign
        /// - "PRIORITY_LEVEL": Call priority
        /// - "DEPARTMENT": Department routing
        /// - "LANGUAGE_PREF": Language preference
        /// - "CALLBACK_NUMBER": Callback number
        /// 
        /// Variable Naming Guidelines:
        /// - Use uppercase for consistency
        /// - Use underscores for word separation
        /// - Avoid spaces and special characters
        /// - Keep names descriptive but concise
        /// - Follow Asterisk naming conventions
        /// - Use function syntax when appropriate: FUNCTION(parameter)
        /// 
        /// Function Variables:
        /// Function variables invoke Asterisk functions when accessed:
        /// - Reading: Get function result
        /// - Writing: Set function parameter
        /// - Examples: CALLERID(name), CDR(userfield), CHANNEL(language)
        /// </remarks>
        public new string Variable { get; set; }

        /// <summary>
        /// Gets or sets the value to assign to the variable.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The variable value to set.
        /// </value>
        /// <remarks>
        /// Value Types and Examples:
        /// 
        /// String Values:
        /// - "John Doe": Text values for names
        /// - "Sales Department": Department names
        /// - "English": Language settings
        /// - "priority": Priority levels
        /// - "": Empty string (clears variable)
        /// 
        /// Numeric Values:
        /// - "1001": Extension numbers
        /// - "30": Timeout values in seconds
        /// - "100": Priority levels
        /// - "5551234567": Phone numbers
        /// - "0": Boolean false equivalent
        /// - "1": Boolean true equivalent
        /// 
        /// Boolean Values:
        /// - "yes" / "no": Asterisk boolean preferences
        /// - "true" / "false": Standard boolean values
        /// - "on" / "off": Feature toggles
        /// - "1" / "0": Numeric boolean equivalents
        /// 
        /// Special Values:
        /// - "${EXTEN}": Dialplan variable substitution
        /// - "${CALLERID(num)}": Function result substitution
        /// - "ulaw,alaw": Codec preferences
        /// - "en_US": Locale identifiers
        /// - "America/New_York": Timezone identifiers
        /// 
        /// CDR Values:
        /// - "ANSWERED": Call disposition
        /// - "NO ANSWER": Call disposition
        /// - "BUSY": Call disposition
        /// - "FAILED": Call disposition
        /// - "DOCUMENTATION": AMA flags
        /// - "BILLING": AMA flags
        /// 
        /// Caller ID Values:
        /// - "John Doe <1001>": Full caller ID format
        /// - "1001": Number only
        /// - "John Doe": Name only
        /// - "Anonymous": Privacy indicator
        /// - "Restricted": Privacy indicator
        /// 
        /// Value Constraints:
        /// - Most variables accept any string value
        /// - Some variables have format requirements
        /// - Function variables may validate input
        /// - Length limits may apply (typically 1024+ characters)
        /// - Special characters usually allowed
        /// 
        /// Value Processing:
        /// - Values are stored as strings internally
        /// - Variable substitution occurs during dialplan execution
        /// - Function variables may transform values
        /// - Empty values effectively unset the variable
        /// 
        /// Security Considerations:
        /// - Values may be logged or visible in events
        /// - Avoid sensitive information in variable values
        /// - Consider encoding for special characters
        /// - Validate input for security applications
        /// </remarks>
        public string Value { get; set; }
    }
}