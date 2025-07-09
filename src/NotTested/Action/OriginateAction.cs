using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events;

namespace AsterNET.Manager.Action
{
    /// <summary>
    /// The OriginateAction generates an outgoing call to the extension in the given
    /// context with the given priority or to a given application with optional
    /// parameters.
    /// If you want to connect to an extension use the properties context, exten and
    /// priority. If you want to connect to an application use the properties
    /// application and data if needed. Note that no call detail record will be
    /// written when directly connecting to an application, so it may be better to
    /// connect to an extension that starts the application you wish to connect to.
    /// The response to this action is sent when the channel has been answered and
    /// asterisk starts connecting it to the given extension. So be careful not to
    /// choose a too short timeout when waiting for the response.
    /// If you set async to true Asterisk reports an OriginateSuccess-
    /// and OriginateFailureEvents. The action id of these events equals the action
    /// id of this OriginateAction.
    /// </summary>
    /// <seealso cref="OriginateSuccessEvent" />
    /// <seealso cref="OriginateFailureEvent" />
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
        public string? Account { get; set; }

        /// <summary>
        /// Gets or sets the caller id to set on the outgoing channel.
        /// </summary>
        public string? CallerId { get; set; }

        /// <summary>
        /// Gets or sets Channel on which to originate the call (The same as you specify in the Dial application command)
        /// This property is required.
        /// </summary>
        public string? Channel { get; set; }

        /// <summary>
        /// Gets or sets originated channel id
        /// </summary>
        public string? ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the name of the context of the extension to connect to.
        /// If you set the context you also have to set the exten and priority properties.
        /// </summary>
        public string? Context { get; set; }

        /// <summary>
        /// Gets or sets the extension to connect to.
        /// If you set the extension you also have to set the context and priority properties.
        /// </summary>
        public string? Exten { get; set; }

        /// <summary>
        /// Gets or sets the priority of the extension to connect to.
        /// If you set the priority you also have to set the context and exten properties.
        /// </summary>
        public string? Priority { get; set; }

        /// <summary>
        /// Gets or sets Application to use on connect (use Data for parameters)
        /// </summary>
        public string? Application { get; set; }

        /// <summary>
        /// Gets or sets the parameters to pass to the application.
        /// Data if Application parameter is user
        /// </summary>
        public string? Data { get; set; }

        /// <summary>
        /// Gets or sets true if this is a fast origination.
        /// For the origination to be asynchronous (allows multiple calls to be generated without waiting for a response).
        /// Will send OriginateSuccess- and OriginateFailureEvents.
        /// </summary>
        public bool Async { get; set; }

        /// <summary>
        /// Gets or sets the timeout for the origination in milliseconds.
        /// The channel must be answered within this time, otherwise the origination
        /// is considered to have failed and an OriginateFailureEvent is generated.
        /// If not set, Asterisk assumes a default value of 30000 meaning 30 seconds.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Returns the event type that indicates completion of the Originate action.
        /// </summary>
        /// <returns>The Type of OriginateResponseEvent</returns>
        public override Type ActionCompleteEventClass()
        {
            return typeof(OriginateResponseEvent);
        }

        /// <summary>
        /// Get the variables dictionary to set on the originated call.
        /// </summary>
        public NameValueCollection GetVariables()
        {
            return Variable ?? new NameValueCollection();
        }

        /// <summary>
        /// Set the variables dictionary to set on the originated call.
        /// </summary>
        public void SetVariables(NameValueCollection vars)
        {
            Variable = vars;
        }

        /// <summary>
        /// Gets a variable on the originated call. Replaces any existing variable with the same name.
        /// </summary>
        public string GetVariable(string key)
        {
            if (Variable == null)
                return string.Empty;
            return Variable[key] ?? string.Empty;
        }

        /// <summary>
        /// Sets a variable dictionary on the originated call. Replaces any existing variable with the same name.
        /// </summary>
        public void SetVariable(string key, string value)
        {
            Variable ??= new NameValueCollection();
            Variable.Set(key, value);
        }
    }
}
