using System;
using Sufficit.Asterisk.Manager.Events;

namespace AsterNET.Manager.Action
{
    /// <summary>
    ///     Show SIP registrations (text format). <br />
    ///     Lists all registration requests and status. Registrations will follow as separate events followed by a final event called 'RegistrationsComplete'.
    /// </summary>
    public class SIPShowRegistryAction : ManagerActionEvent
    {
        public override string Action 
            => "SIPshowregistry";
        
        public override Type ActionCompleteEventClass()
            => typeof (RegistrationsCompleteEvent);        
    }
}