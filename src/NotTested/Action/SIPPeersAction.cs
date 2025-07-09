using System;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events;

namespace AsterNET.Manager.Action
{
    /// <summary>
    /// The SIPPeersAction requests the state of all SIP peers.
    /// For each SIP peer a PeerEntryEvent is generated. After the state of all peers has been 
    /// reported a PeerlistCompleteEvent is generated.
    /// Available since Asterisk 1.2
    /// </summary>
    /// <seealso cref="PeerEntryEvent" />
    /// <seealso cref="PeerlistCompleteEvent" />
    public class SIPPeersAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "SIPPeers"</value>
        public override string Action => "SIPPeers";

        /// <summary>
        /// Returns the event type that indicates completion of the SIPPeers action.
        /// </summary>
        /// <returns>The Type of PeerlistCompleteEvent</returns>
        public override Type ActionCompleteEventClass()
        {
            return typeof(PeerlistCompleteEvent);
        }
    }
}