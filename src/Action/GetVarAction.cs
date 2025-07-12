using Sufficit.Asterisk.Manager.Response;
using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    ///     The GetVarAction queries for a channel variable.
    ///     This action allows reading channel variables that have been set during call execution.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Getvar
    /// Purpose: Retrieve channel variable values programmatically
    /// Privilege Required: call,all
    /// 
    /// Required Parameters:
    /// - Channel: The channel to get the variable from (Required)
    /// - Variable: The name of the variable to retrieve (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Response Fields:
    /// - Response: Success/Error
    /// - ActionID: Correlates with request
    /// - Variable: Variable name that was requested
    /// - Value: Current value of the variable
    /// 
    /// Variable Scope:
    /// - Channel variables: Specific to the channel
    /// - Inherited variables: May come from parent channels
    /// - Global variables: Use Global() function syntax
    /// - Function variables: Invoke functions to get values
    /// 
    /// Variable Types:
    /// - Built-in: Asterisk system variables (CHANNEL, CALLERID, etc.)
    /// - Custom: User-defined variables for application logic
    /// - Function: Variables that invoke functions (CALLERID(name))
    /// - Special: Variables with dynamic behavior (CDR, CONNECTEDLINE)
    /// 
    /// Usage Scenarios:
    /// - Call routing decision making
    /// - Retrieving caller identification
    /// - Reading call state information
    /// - Application state inspection
    /// - CDR field examination
    /// - Call monitoring and logging
    /// - Interactive voice response (IVR) state
    /// - Call center data collection
    /// 
    /// Common Variables to Retrieve:
    /// - CALLERID(name): Caller ID name
    /// - CALLERID(num): Caller ID number
    /// - CHANNEL(state): Current channel state
    /// - CDR(accountcode): Account code for billing
    /// - UNIQUEID: Unique call identifier
    /// - EXTEN: Current extension
    /// - CONTEXT: Current context
    /// - PRIORITY: Current priority
    /// 
    /// Asterisk Versions:
    /// - Available since early Asterisk versions
    /// - Function variable support added in later versions
    /// - Enhanced error handling in modern versions
    /// - Consistent behavior across versions
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// Variables are retrieved from channel's variable list.
    /// Function variables may execute code when retrieved.
    /// Non-existent variables return empty value.
    /// 
    /// Security Considerations:
    /// - Variables may contain sensitive information
    /// - Some variables reveal system internals
    /// - Consider authorization for critical variables
    /// - Monitor access to personal data variables
    /// 
    /// Error Conditions:
    /// - Channel not found
    /// - Channel not accessible
    /// - Invalid variable name format
    /// - Insufficient privileges
    /// 
    /// Example Usage:
    /// <code>
    /// // Get caller ID name
    /// var getVar = new GetVarAction(
    ///     "SIP/1001-00000001", 
    ///     "CALLERID(name)"
    /// );
    /// 
    /// // Get account code
    /// var getVar = new GetVarAction(
    ///     "SIP/1001-00000001", 
    ///     "CDR(accountcode)"
    /// );
    /// 
    /// // Get custom variable
    /// var getVar = new GetVarAction(
    ///     "SIP/1001-00000001", 
    ///     "CUSTOMER_ID"
    /// );
    /// </code>
    /// </remarks>
    /// <seealso cref="SetVarAction"/>
    /// <seealso cref="StatusAction"/>
    public class GetVarAction : ManagerAction
    {
        /// <summary>
        ///     Creates a new GetVarAction that queries for the given global variable.
        /// </summary>
        /// <param name="variable">the name of the global variable to query.</param>
        public GetVarAction(string variable) : this(default!, variable) { }

        /// <summary>
        ///     Creates a new GetVarAction that queries for the given local channel variable.
        /// </summary>
        /// <param name="channel">the name of the channel, for example "SIP/1234-9cd".</param>
        /// <param name="variable">the name of the variable to query.</param>
        /// <remarks>
        /// Parameter Requirements:
        /// - Channel: Must be valid, active channel
        /// - Variable: Must be valid variable name
        /// 
        /// Variable naming:
        /// - Use exact case as when variable was set
        /// - Function syntax: FUNCTION(parameter)
        /// - Built-in variables follow Asterisk conventions
        /// </remarks>
        public GetVarAction (string channel, string variable)
        {
            Channel = channel;
            VariableName = variable;
        }

        /// <summary>
        ///     Creates a new GetVarAction with action ID for correlation.
        /// </summary>
        /// <param name="channel">The channel to get the variable from (Required)</param>
        /// <param name="variable">The name of the variable to retrieve (Required)</param>
        /// <param name="actionId">Unique identifier for this action (Optional)</param>
        /// <remarks>
        /// The action ID allows correlation of responses with this specific request.
        /// Useful for tracking the success/failure of variable retrieval.
        /// </remarks>
        public GetVarAction(string channel, string variable, string actionId) 
            : this(channel, variable)
        {
            ActionId = actionId;
        }

        /// <summary>
        ///     Get the name of this action, i.e. "GetVar".
        /// </summary>
        public override string Action => "Getvar";

        /// <summary>
        ///     Get/Set the name of the channel, if you query for a local channel variable.
        ///     Leave empty to query for a global variable.
        /// </summary>
        /// <remarks>
        /// Channel Requirements:
        /// - Must be a valid, active channel
        /// - Channel must be accessible for variable operations
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
        /// - Some variables may be inherited from parent channels
        /// - Built-in variables are always available
        /// - Custom variables depend on previous SetVar operations
        /// 
        /// The channel must exist and be accessible for variable operations.
        /// Use Status action or channel events to verify channel existence.
        /// </remarks>
        public string Channel { get; set; } = default!;

        /// <summary>
        ///     Get/Set the name of the variable to query.
        /// </summary>
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
        /// - "CHANNEL(stack-peek(1))": Gosub stack level
        /// - "CHANNEL(trace)": Channel trace flag
        /// 
        /// Caller ID Variables:
        /// - "CALLERID(name)": Caller ID name
        /// - "CALLERID(num)": Caller ID number  
        /// - "CALLERID(ani)": Automatic Number Identification
        /// - "CALLERID(ani2)": ANI II digits
        /// - "CALLERID(rdnis)": Redirecting Directory Number
        /// - "CALLERID(dnid)": Dialed Number Identification
        /// - "CALLERID(all)": Complete caller ID string
        /// - "CALLERID(pres)": Presentation indicator
        /// - "CALLERID(ton)": Type of number
        /// - "CALLERID(tns)": Transit Network Selection
        /// 
        /// Connected Line Variables:
        /// - "CONNECTEDLINE(name)": Connected line name
        /// - "CONNECTEDLINE(num)": Connected line number
        /// - "CONNECTEDLINE(pres)": Presentation indicator
        /// - "CONNECTEDLINE(ton)": Type of number
        /// - "CONNECTEDLINE(all)": Complete connected line info
        /// 
        /// Redirecting Variables:
        /// - "REDIRECTING(from-name)": Original caller name
        /// - "REDIRECTING(from-num)": Original caller number
        /// - "REDIRECTING(to-name)": Final destination name
        /// - "REDIRECTING(to-num)": Final destination number
        /// - "REDIRECTING(reason)": Redirection reason
        /// - "REDIRECTING(count)": Redirect count
        /// 
        /// Dialplan Variables:
        /// - "CONTEXT": Current context
        /// - "EXTEN": Current extension
        /// - "PRIORITY": Current priority
        /// - "UNIQUEID": Unique call identifier
        /// - "TIMESTAMP": Call timestamp
        /// - "ACCOUNTCODE": Account code
        /// - "BLINDTRANSFER": Blind transfer indicator
        /// - "ATTENDEDTRANSFER": Attended transfer indicator
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
        /// - "CDR(start)": Call start time
        /// - "CDR(answer)": Call answer time
        /// - "CDR(end)": Call end time
        /// - "CDR(duration)": Call duration
        /// - "CDR(billsec)": Billable seconds
        /// - "CDR(disposition)": Call disposition
        /// - "CDR(amaflags)": AMA flags
        /// - "CDR(uniqueid)": Unique call identifier
        /// - "CDR(userfield)": User-defined field
        /// 
        /// System Variables:
        /// - "ASTVERSION": Asterisk version
        /// - "SYSTEMNAME": System name
        /// - "EPOCH": Current Unix timestamp
        /// - "DATETIME": Current date/time
        /// - "STRFTIME": Formatted date/time
        /// - "ENV(variable)": Environment variable
        /// 
        /// Call State Variables:
        /// - "HANGUPCAUSE": Hangup cause code
        /// - "HANGUPCAUSE_KEYS": Available hangup causes
        /// - "DIALEDTIME": Time spent dialing
        /// - "ANSWEREDTIME": Time since answer
        /// - "DIALSTATUS": Last dial status
        /// - "PRIREDIRECTREASON": PRI redirect reason
        /// 
        /// Custom Variables:
        /// Variables set by applications or SetVar action:
        /// - "CUSTOMER_ID": Customer identifier
        /// - "CAMPAIGN_NAME": Marketing campaign
        /// - "PRIORITY_LEVEL": Call priority
        /// - "DEPARTMENT": Department routing
        /// - "LANGUAGE_PREF": Language preference
        /// - "CALLBACK_NUMBER": Callback number
        /// - "RECORDING_ID": Recording identifier
        /// - "TRANSFER_SOURCE": Transfer origination
        /// 
        /// Variable Naming Guidelines:
        /// - Variable names are case-sensitive
        /// - Use exact case as when variable was set
        /// - Function syntax: FUNCTION(parameter)
        /// - Built-in variables follow Asterisk conventions
        /// - Custom variables typically use uppercase
        /// 
        /// Function Variables:
        /// Function variables invoke Asterisk functions when accessed:
        /// - May execute code to generate values
        /// - Examples: CALLERID(name), CDR(userfield), CHANNEL(language)
        /// - Can have side effects when read
        /// - May return different values on successive calls
        /// 
        /// Non-existent Variables:
        /// - Return empty string value
        /// - No error generated for missing variables
        /// - Check response value to determine if variable exists
        /// - Function variables may return default values
        /// </remarks>
        public string VariableName { get; set; } = default!;
    }
}