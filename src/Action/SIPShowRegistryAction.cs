using System;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The SIPShowRegistryAction requests the state of all SIP registry entries.
    /// This action shows Asterisk's registration status with external SIP providers and trunks.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: SIPShowRegistry
    /// Purpose: Retrieve information about SIP registrations to external providers
    /// Privilege Required: system,reporting,all
    /// 
    /// Response Flow:
    /// 1. RegistryEntryEvent: Information about each SIP registry entry
    /// 2. RegistrationsCompleteEvent: Indicates end of registry list
    /// 
    /// Registry Information Included:
    /// - Registration URI and proxy
    /// - Username and authentication realm
    /// - Registration state (Registered/Unregistered/Failed/Timeout)
    /// - Registration expiration time
    /// - Next registration refresh time
    /// - Last registration attempt result
    /// - Contact information and port
    /// 
    /// Registry States:
    /// - "Registered" - Successfully registered with provider
    /// - "Unregistered" - Not currently registered
    /// - "Failed" - Registration failed (auth, network, etc.)
    /// - "Timeout" - Registration timed out
    /// - "Rejected" - Provider rejected registration
    /// - "Request Sent" - Registration in progress
    /// 
    /// Usage Scenarios:
    /// - SIP trunk monitoring
    /// - Provider connectivity verification
    /// - Troubleshooting outbound call issues
    /// - Registration status reporting
    /// - Network connectivity diagnostics
    /// - Service availability monitoring
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.2
    /// - Enhanced with additional registry information in later versions
    /// - PJSIP equivalent: PJSIPShowRegistrations action (Asterisk 12+)
    /// 
    /// Implementation Notes:
    /// This action is implemented in channels/chan_sip.c in Asterisk source code.
    /// Shows only chan_sip registrations, not PJSIP registrations.
    /// For PJSIP, use PJSIPShowRegistrations instead.
    /// 
    /// Configuration Reference:
    /// Registry entries are configured in sip.conf:
    /// register => user:secret@provider.com/extension
    /// 
    /// Example Response Sequence:
    /// 1. Response: Success
    /// 2. RegistryEntryEvent (for each registry entry)
    /// 3. RegistrationsCompleteEvent (completion marker)
    /// 
    /// Common Issues:
    /// - Authentication failures (wrong credentials)
    /// - Network connectivity problems
    /// - Firewall/NAT configuration issues
    /// - Provider service outages
    /// - DNS resolution problems
    /// </remarks>
    /// <seealso cref="RegistryEntryEvent"/>
    /// <seealso cref="RegistrationsCompleteEvent"/>
    public class SIPShowRegistryAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "SIPshowregistry"</value>
        public override string Action => "SIPshowregistry";

        /// <summary>
        /// Returns the event type that indicates completion of the SIPShowRegistry action.
        /// </summary>
        /// <returns>The Type of RegistrationsCompleteEvent</returns>
        /// <remarks>
        /// The RegistrationsCompleteEvent is sent by Asterisk to indicate that all
        /// SIP registry information has been transmitted. This event marks the
        /// end of the response sequence for this action.
        /// 
        /// The completion event typically contains:
        /// - Total number of registry entries
        /// - Count of successful/failed registrations
        /// - Summary statistics
        /// </remarks>
        public override Type ActionCompleteEventClass()
        {
            return typeof(RegistrationsCompleteEvent);
        }
    }
}