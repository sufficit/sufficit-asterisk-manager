using System;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The SIPPeersAction requests the state of all SIP peers (endpoints) in Asterisk.
    /// This action provides comprehensive information about SIP peer configuration and status.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: SIPPeers
    /// Purpose: Retrieve detailed information about all SIP peers/endpoints
    /// Privilege Required: system,reporting,all
    /// 
    /// Response Flow:
    /// 1. PeerEntryEvent: Information about each SIP peer
    /// 2. PeerlistCompleteEvent: Indicates end of peer list
    /// 
    /// Peer Information Included:
    /// - Peer name and contact information
    /// - Registration status (Registered/Unregistered)
    /// - IP address and port
    /// - User agent information
    /// - Codec capabilities
    /// - Registration expiration
    /// - Qualify status (reachability)
    /// - Security settings (encryption, authentication)
    /// - Call limits and restrictions
    /// 
    /// Usage Scenarios:
    /// - SIP endpoint monitoring
    /// - Registration status tracking
    /// - Network topology discovery
    /// - Troubleshooting connectivity issues
    /// - Capacity planning and reporting
    /// - Security auditing
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.2
    /// - Enhanced with additional peer information in later versions
    /// - PJSIP equivalent: PJSIPShowEndpoints action (Asterisk 12+)
    /// 
    /// Implementation Notes:
    /// This action is implemented in channels/chan_sip.c in Asterisk source code.
    /// For systems with many SIP peers, this can generate numerous events.
    /// Consider using peer-specific queries for targeted monitoring.
    /// 
    /// PJSIP Migration:
    /// - For Asterisk 12+, consider using PJSIPShowEndpoints for PJSIP endpoints
    /// - SIPPeers only shows chan_sip peers, not PJSIP endpoints
    /// - Use appropriate action based on your SIP channel driver
    /// 
    /// Example Response Sequence:
    /// 1. Response: Success
    /// 2. PeerEntryEvent (for each SIP peer)
    /// 3. PeerlistCompleteEvent (completion marker)
    /// 
    /// Common Peer States:
    /// - "Registered" - Peer is registered and reachable
    /// - "Unregistered" - Peer is not registered
    /// - "Unreachable" - Peer is registered but not responding to qualify
    /// - "Lagged" - Peer is responding slowly to qualify packets
    /// </remarks>
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
        /// <remarks>
        /// The PeerlistCompleteEvent is sent by Asterisk to indicate that all
        /// SIP peer information has been transmitted. This event marks the
        /// end of the response sequence for this action.
        /// 
        /// The completion event typically contains:
        /// - Total number of SIP peers
        /// - Count of online/offline peers
        /// - System statistics
        /// </remarks>
        public override Type ActionCompleteEventClass()
        {
            return typeof(PeerlistCompleteEvent);
        }
    }
}