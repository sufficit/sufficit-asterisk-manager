using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The OriginateAction generates an outgoing call to the extension in the given
    /// context with the given priority or to a given application with optional
    /// parameters.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Originate
    /// Purpose: Initiate an outgoing call programmatically
    /// Privilege Required: originate
    /// 
    /// Call Destination Options:
    /// 1. Dialplan: Use Context, Exten, and Priority to connect to a dialplan location
    /// 2. Application: Use Application and Data to connect directly to an application
    /// 
    /// Important Notes:
    /// - When connecting to an extension, a call detail record (CDR) will be written
    /// - When connecting directly to an application, no CDR is written by default
    /// - For CDR recording with applications, connect to an extension that starts the application
    /// - The response is sent when the channel is answered and connection begins
    /// - Use appropriate timeout values to avoid premature failure
    /// 
    /// Asynchronous Operation:
    /// - Set Async=true for non-blocking operation
    /// - Async mode generates OriginateSuccess and OriginateFailure events
    /// - Action ID correlates the response events with this action
    /// 
    /// Channel Variables:
    /// - Set channel variables using the Variable collection
    /// - Variables are available in the dialplan or application
    /// - Format: Variable["NAME"] = "VALUE"
    /// 
    /// Usage Scenarios:
    /// - Click-to-call functionality
    /// - Automated outbound calling
    /// - Call bridging and conferencing
    /// - Interactive voice response (IVR) systems
    /// - Callback implementations
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.0
    /// - Async parameter added in later versions
    /// - Enhanced with additional options in newer versions
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// The channel must be answered within the timeout period or the action fails.
    /// Default timeout is 30 seconds (30000 milliseconds) if not specified.
    /// 
    /// Example Usage:
    /// <code>
    /// var originate = new OriginateAction
    /// {
    ///     Channel = "SIP/1001",
    ///     Context = "default",
    ///     Exten = "1000",
    ///     Priority = "1",
    ///     CallerId = "Sales &lt;2000&gt;",
    ///     Timeout = 30000,
    ///     Async = true
    /// };
    /// originate.SetVariable("CALLERID(name)", "Sales Department");
    /// </code>
    /// </remarks>
    /// <seealso cref="OriginateSuccessEvent" />
    /// <seealso cref="OriginateFailureEvent" />
    /// <seealso cref="OriginateResponseEvent" />
    public class OriginateAction : ManagerActionEvent, IActionVariable
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Originate"</value>
        public override string Action => "Originate";

        /// <summary>
        /// Variable: concat all items here ... 
        /// Can have multiple keys with the same name
        /// </summary>
        public new NameValueCollection? Variable { get; set; }

        /// <summary>
        /// Gets or sets the account code to use for the originated call.
        /// The account code is included in the call detail record generated for this call and will be used for billing.
        /// </summary>
        /// <value>
        /// Account code string for billing and CDR purposes.
        /// </value>
        /// <remarks>
        /// The account code appears in:
        /// - Call Detail Records (CDR)
        /// - Channel events (accountcode field)
        /// - Billing and reporting systems
        /// 
        /// Common usage:
        /// - Customer identification
        /// - Department billing codes  
        /// - Call classification
        /// - Rate plan identification
        /// </remarks>
        public string? Account { get; set; }

        /// <summary>
        /// Gets or sets the caller id to set on the outgoing channel.
        /// </summary>
        /// <value>
        /// Caller ID in format "Name &lt;Number&gt;" or just "Number".
        /// </value>
        /// <remarks>
        /// Caller ID format examples:
        /// - "John Doe &lt;1001&gt;" (name and number)
        /// - "Sales Department &lt;2000&gt;" (department name)
        /// - "1001" (number only)
        /// - "&lt;1001&gt;" (number only with brackets)
        /// 
        /// The caller ID affects:
        /// - What the called party sees
        /// - Call routing decisions
        /// - Call logs and CDRs
        /// - Privacy and identification settings
        /// </remarks>
        public string? CallerId { get; set; }

        /// <summary>
        /// Gets or sets Channel on which to originate the call (The same as you specify in the Dial application command)
        /// This property is required.
        /// </summary>
        /// <value>
        /// Channel specification in Asterisk format.
        /// </value>
        /// <remarks>
        /// Channel format examples:
        /// - "SIP/1001" (SIP endpoint)
        /// - "IAX2/provider/5551234567" (IAX trunk)
        /// - "DAHDI/1" (DAHDI channel)
        /// - "Local/1001@from-internal" (Local channel)
        /// - "PJSIP/1001" (PJSIP endpoint)
        /// 
        /// This is the source channel that will be created and connected.
        /// The channel technology and resource must be properly configured.
        /// </remarks>
        public string? Channel { get; set; }

        /// <summary>
        /// Gets or sets the originated channel id
        /// </summary>
        /// <value>
        /// Unique identifier for the originated channel.
        /// </value>
        /// <remarks>
        /// The channel ID allows tracking of the specific channel:
        /// - Appears in subsequent events
        /// - Used for channel-specific operations
        /// - Correlation with other manager actions
        /// - If not specified, Asterisk generates one automatically
        /// </remarks>
        public string? ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the name of the context of the extension to connect to.
        /// If you set the context you also have to set the exten and priority properties.
        /// </summary>
        /// <value>
        /// Dialplan context name.
        /// </value>
        /// <remarks>
        /// Context requirements:
        /// - Must exist in dialplan configuration
        /// - Required when using Exten and Priority
        /// - Mutually exclusive with Application/Data
        /// 
        /// Common contexts:
        /// - "default" (default context)
        /// - "from-internal" (internal calls)
        /// - "from-trunk" (incoming calls)
        /// - "outbound-routing" (outgoing calls)
        /// </remarks>
        public string? Context { get; set; }

        /// <summary>
        /// Gets or sets the extension to connect to.
        /// If you set the extension you also have to set the context and priority properties.
        /// </summary>
        /// <value>
        /// Dialplan extension.
        /// </value>
        /// <remarks>
        /// Extension examples:
        /// - "1001" (specific extension)
        /// - "s" (start extension)
        /// - "_1NXXNXXXXXX" (pattern match)
        /// - "h" (hangup extension)
        /// 
        /// The extension must exist in the specified context.
        /// Can be literal extensions or pattern matches.
        /// </remarks>
        public string? Exten { get; set; }

        /// <summary>
        /// Gets or sets the priority of the extension to connect to.
        /// If you set the priority you also have to set the context and exten properties.
        /// </summary>
        /// <value>
        /// Dialplan priority (usually "1" to start).
        /// </value>
        /// <remarks>
        /// Priority guidelines:
        /// - Usually starts at "1"
        /// - Can be numeric or named labels
        /// - "n" means next priority
        /// - Must exist in the specified context/extension
        /// 
        /// Examples:
        /// - "1" (first priority)
        /// - "start" (named priority)
        /// - "n+101" (jump ahead)
        /// </remarks>
        public string? Priority { get; set; }

        /// <summary>
        /// Gets or sets Application to use on connect (use Data for parameters)
        /// </summary>
        /// <value>
        /// Asterisk application name.
        /// </value>
        /// <remarks>
        /// Application examples:
        /// - "Playback" (play audio file)
        /// - "VoiceMailMain" (voicemail access)
        /// - "MeetMe" (conference bridge)
        /// - "Queue" (call queue)
        /// - "Echo" (echo test)
        /// 
        /// When using Application:
        /// - Mutually exclusive with Context/Exten/Priority
        /// - Use Data property for application parameters
        /// - No CDR generated by default
        /// - Application must be loaded and available
        /// </remarks>
        public string? Application { get; set; }

        /// <summary>
        /// Gets or sets the parameters to pass to the application.
        /// Data if Application parameter is used
        /// </summary>
        /// <value>
        /// Application-specific parameters.
        /// </value>
        /// <remarks>
        /// Data format depends on the application:
        /// - Playback: "welcome" (filename)
        /// - VoiceMailMain: "1001@default" (mailbox@context)
        /// - MeetMe: "1000,M" (room,options)
        /// - Queue: "support,t" (queue,options)
        /// 
        /// Multiple parameters are typically separated by commas.
        /// Refer to application documentation for specific syntax.
        /// </remarks>
        public string? Data { get; set; }

        /// <summary>
        /// Gets or sets true if this is a fast origination.
        /// For the origination to be asynchronous (allows multiple calls to be generated without waiting for a response).
        /// Will send OriginateSuccess- and OriginateFailureEvents.
        /// </summary>
        /// <value>
        /// True for asynchronous operation, false for synchronous.
        /// </value>
        /// <remarks>
        /// Synchronous mode (Async = false):
        /// - Blocks until origination completes or fails
        /// - Returns success/failure in immediate response
        /// - Simpler error handling
        /// - Lower throughput for multiple calls
        /// 
        /// Asynchronous mode (Async = true):
        /// - Returns immediately with action acknowledgment
        /// - Final result in OriginateSuccess/OriginateFailure events
        /// - Higher throughput for multiple simultaneous calls
        /// - Requires event handling for completion
        /// - Events correlated by ActionID
        /// </remarks>
        public bool Async { get; set; }

        /// <summary>
        /// Gets or sets the timeout for the origination in milliseconds.
        /// The channel must be answered within this time, otherwise the origination
        /// is considered to have failed and an OriginateFailureEvent is generated.
        /// If not set, Asterisk assumes a default value of 30000 meaning 30 seconds.
        /// </summary>
        /// <value>
        /// Timeout in milliseconds (default: 30000).
        /// </value>
        /// <remarks>
        /// Timeout considerations:
        /// - Too short: Legitimate calls may be cut off
        /// - Too long: Failed calls tie up resources
        /// - Mobile phones may need longer timeouts
        /// - PSTN calls typically answer within 30-60 seconds
        /// 
        /// Timeout values:
        /// - 15000 (15 seconds) - aggressive
        /// - 30000 (30 seconds) - default
        /// - 60000 (60 seconds) - patient
        /// - 0 = infinite timeout (not recommended)
        /// </remarks>
        public int Timeout { get; set; }

        /// <summary>
        /// Returns the event type that indicates completion of the Originate action.
        /// </summary>
        /// <returns>The Type of OriginateResponseEvent</returns>
        /// <remarks>
        /// Completion events:
        /// - Synchronous mode: OriginateResponseEvent
        /// - Asynchronous mode: OriginateSuccessEvent or OriginateFailureEvent
        /// 
        /// Event correlation:
        /// - All events include the ActionID from this action
        /// - Use ActionID to match events to origination requests
        /// - Events contain channel information and result details
        /// </remarks>
        public override Type ActionCompleteEventClass()
        {
            return typeof(OriginateResponseEvent);
        }

        /// <summary>
        /// Get the variables dictionary to set on the originated call.
        /// </summary>
        /// <returns>NameValueCollection of channel variables</returns>
        /// <remarks>
        /// Channel variables:
        /// - Available in dialplan as ${VARIABLE_NAME}
        /// - Passed to applications as parameters
        /// - Visible in events and CDRs
        /// - Can influence call routing and behavior
        /// 
        /// Example variables:
        /// - CALLERID(name): Override caller name
        /// - MONITOR_EXEC: Enable call recording
        /// - CHANNEL(accountcode): Set account code
        /// - Custom variables for application logic
        /// </remarks>
        public NameValueCollection GetVariables()
        {
            return Variable ?? new NameValueCollection();
        }

        /// <summary>
        /// Set the variables dictionary to set on the originated call.
        /// </summary>
        /// <param name="vars">Collection of variables to set</param>
        /// <remarks>
        /// This replaces all existing variables with the provided collection.
        /// Use SetVariable() to add individual variables while preserving existing ones.
        /// </remarks>
        public void SetVariables(NameValueCollection vars)
        {
            Variable = vars;
        }

        /// <summary>
        /// Gets a variable on the originated call.
        /// </summary>
        /// <param name="key">Variable name</param>
        /// <returns>Variable value or empty string if not found</returns>
        /// <remarks>
        /// Variable names are case-sensitive.
        /// Returns empty string if variable is not set or collection is null.
        /// </remarks>
        public string GetVariable(string key)
        {
            if (Variable == null)
                return string.Empty;
            return Variable[key] ?? string.Empty;
        }

        /// <summary>
        /// Sets a variable on the originated call. Replaces any existing variable with the same name.
        /// </summary>
        /// <param name="key">Variable name</param>
        /// <param name="value">Variable value</param>
        /// <remarks>
        /// Variable naming conventions:
        /// - Use uppercase for consistency with Asterisk
        /// - Avoid spaces and special characters
        /// - Use underscore for word separation
        /// 
        /// Example variable names:
        /// - CUSTOMER_ID
        /// - CAMPAIGN_NAME
        /// - PRIORITY_LEVEL
        /// - CUSTOM_FIELD_1
        /// </remarks>
        public void SetVariable(string key, string value)
        {
            Variable ??= new NameValueCollection();
            Variable.Set(key, value);
        }
    }
}