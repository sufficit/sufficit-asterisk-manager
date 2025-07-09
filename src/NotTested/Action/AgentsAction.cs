using System;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events;

namespace AsterNET.Manager.Action
{
    /// <summary>
    /// The AgentsAction requests the state of all agents.
    /// For each agent an AgentsEvent is generated.
    /// After the state of all agents has been reported an AgentsCompleteEvent is generated.
    /// Available since Asterisk 1.2
    /// </summary>
    /// <seealso cref="AgentsEvent" />
    /// <seealso cref="AgentsCompleteEvent" />
    public class AgentsAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Agents"</value>
        public override string Action => "Agents";

        /// <summary>
        /// Returns the event type that indicates completion of the Agents action.
        /// </summary>
        /// <returns>The Type of AgentsCompleteEvent</returns>
        public override Type ActionCompleteEventClass()
        {
            return typeof(AgentsCompleteEvent);
        }
    }
}