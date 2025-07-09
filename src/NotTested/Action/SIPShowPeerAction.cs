using System;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events;

namespace AsterNET.Manager.Action
{
    /// <summary>
    /// The SIPShowPeerAction requests information about a specific SIP peer.
    /// For each SIP peer a SIPShowPeerEvent is generated. After all information has been 
    /// reported a PeerlistCompleteEvent is generated.
    /// Available since Asterisk 1.2
    /// </summary>
    /// <seealso cref="PeerEntryEvent" />
    /// <seealso cref="PeerlistCompleteEvent" />
    public class SIPShowPeerAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "SIPShowPeer"</value>
        public override string Action => "SIPShowPeer";

        /// <summary>
        /// Gets or sets the name of the SIP peer to show information for.
        /// </summary>
        public string? Peer { get; set; }

        /// <summary>
        /// Returns the event type that indicates completion of the SIPShowPeer action.
        /// </summary>
        /// <returns>The Type of PeerlistCompleteEvent</returns>
        public override Type ActionCompleteEventClass()
        {
            return typeof(PeerlistCompleteEvent);
        }
    }
}