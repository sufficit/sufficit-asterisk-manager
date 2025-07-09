using System;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events;

namespace AsterNET.Manager.Action
{
    /// <summary>
    /// The ConfbridgeListAction requests all current ConfBridge conferences.
    /// For each conference a ConfbridgeListEvent is generated. After all conferences have been listed
    /// a ConfbridgeListCompleteEvent is generated.
    /// Available since Asterisk 10.0
    /// </summary>
    /// <seealso cref="ConfbridgeListEvent" />
    /// <seealso cref="ConfbridgeListCompleteEvent" />
    public class ConfbridgeListAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "ConfbridgeList"</value>
        public override string Action => "ConfbridgeList";

        /// <summary>
        /// Gets or sets the conference identifier filter.
        /// When specified, only information about the named conference is returned.
        /// </summary>
        public string? Conference { get; set; }

        /// <summary>
        /// Returns the event type that indicates completion of the ConfbridgeList action.
        /// </summary>
        /// <returns>The Type of ConfbridgeListCompleteEvent</returns>
        public override Type ActionCompleteEventClass()
        {
            return typeof(ConfbridgeListCompleteEvent);
        }
    }
}