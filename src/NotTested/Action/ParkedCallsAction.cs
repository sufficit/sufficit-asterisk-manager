using System;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events;

namespace AsterNET.Manager.Action
{
    /// <summary>
    /// The ParkedCallsAction requests a list of all currently parked calls.
    /// For each active channel a ParkedCallEvent is generated. After all parked
    /// calls have been reported a ParkedCallsCompleteEvent is generated.
    /// </summary>
    /// <seealso cref="ParkedCallEvent" />
    /// <seealso cref="ParkedCallsCompleteEvent" />
    public class ParkedCallsAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "ParkedCalls"</value>
        public override string Action => "ParkedCalls";

        /// <summary>
        /// Returns the event type that indicates completion of the ParkedCalls action.
        /// </summary>
        /// <returns>The Type of ParkedCallsCompleteEvent</returns>
        public override Type ActionCompleteEventClass()
        {
            return typeof(ParkedCallsCompleteEvent);
        }
    }
}