using System;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events;

namespace AsterNET.Manager.Action
{
    /// <summary>
    ///     Show SIP registrations (text format).
    ///     Lists all registration requests and status. Registrations will follow as separate events followed by a final event called 'RegistrationsComplete'.
    ///     Available since Asterisk 1.2
    /// </summary>
    /// <seealso cref="RegistryEvent" />
    /// <seealso cref="RegistrationsCompleteEvent" />
    public class SIPShowRegistryAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "SIPshowregistry"</value>
        public override string Action => "SIPshowregistry";

        /// <summary>
        /// Returns the event type that indicates completion of the SIPshowregistry action.
        /// </summary>
        /// <returns>The Type of RegistrationsCompleteEvent</returns>
        public override Type ActionCompleteEventClass()
        {
            return typeof(RegistrationsCompleteEvent);
        }
    }
}