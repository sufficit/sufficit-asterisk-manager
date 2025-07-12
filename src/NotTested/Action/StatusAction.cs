using System;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The StatusAction requests the state of all active channels.
    /// For each active channel a StatusEvent is generated. After the state of all
    /// channels has been reported a StatusCompleteEvent is generated.
    /// </summary>
    /// <seealso cref="StatusEvent" />
    /// <seealso cref="StatusCompleteEvent" />
    public class StatusAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Status"</value>
        public override string Action => "Status";

        /// <summary>
        /// Returns the event type that indicates completion of the Status action.
        /// </summary>
        /// <returns>The Type of StatusCompleteEvent</returns>
        public override Type ActionCompleteEventClass()
        {
            return typeof(StatusCompleteEvent);
        }
    }
}