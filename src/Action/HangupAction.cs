using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The HangupAction hangs up (terminates) a specific channel.
    /// This action immediately terminates the call on the specified channel.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Hangup
    /// Purpose: Terminate a specific channel/call
    /// Privilege Required: call,all
    /// 
    /// Required Parameters:
    /// - Channel: The channel to hangup (Required)
    /// 
    /// Optional Parameters:
    /// - Cause: Hangup cause code (Optional)
    /// 
    /// Hangup Cause Codes (ITU-T Q.850):
    /// - 16: Normal call clearing (default)
    /// - 17: User busy  
    /// - 18: No user responding
    /// - 19: No answer from user
    /// - 21: Call rejected
    /// - 26: Non-selected user clearing
    /// - 27: Destination out of order
    /// - 28: Invalid number format
    /// - 34: No circuit/channel available
    /// - 38: Network out of order
    /// - 41: Temporary failure
    /// - 42: Switching equipment congestion
    /// - 44: Requested circuit/channel not available
    /// - 50: Requested facility not subscribed
    /// - 58: Bearer capability not presently available
    /// - 102: Recovery on timer expiry
    /// 
    /// Usage Scenarios:
    /// - Emergency call termination
    /// - Administrative call control
    /// - Call transfer preparation
    /// - Queue timeout handling
    /// - Security-related disconnections
    /// - Resource management
    /// 
    /// Asterisk Versions:
    /// - Available since early Asterisk versions
    /// - Cause parameter added in later versions
    /// - Consistent behavior across versions
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// The hangup is immediate and cannot be undone.
    /// Channel must exist and be active.
    /// 
    /// Security Considerations:
    /// - Requires appropriate manager privileges
    /// - Can terminate active calls
    /// - Should be restricted to authorized users
    /// - Consider logging for audit purposes
    /// 
    /// Example Usage:
    /// <code>
    /// // Basic hangup
    /// var hangup = new HangupAction("SIP/1001-00000001");
    /// 
    /// // Hangup with specific cause
    /// var hangup = new HangupAction("SIP/1001-00000001", 16); // Normal clearing
    /// </code>
    /// </remarks>
    /// <seealso cref="OriginateAction"/>
    /// <seealso cref="BridgeAction"/>
    public class HangupAction : ManagerAction
    {
        /// <summary>
        /// Creates a new HangupAction for the specified channel.
        /// </summary>
        /// <param name="channel">The channel to hangup (Required)</param>
        /// <remarks>
        /// Channel format examples:
        /// - "SIP/1001-00000001" (SIP channel)
        /// - "IAX2/provider-00000001" (IAX2 channel)
        /// - "DAHDI/1-1" (DAHDI channel)
        /// - "Local/1001@from-internal-00000001;1" (Local channel)
        /// - "PJSIP/1001-00000001" (PJSIP channel)
        /// 
        /// The channel name can be obtained from:
        /// - Channel events (Newchannel, etc.)
        /// - Status action responses
        /// - Other manager actions that return channel information
        /// </remarks>
        public HangupAction(string channel)
        {
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        /// <summary>
        /// Creates a new HangupAction for the specified channel with a specific cause code.
        /// </summary>
        /// <param name="channel">The channel to hangup (Required)</param>
        /// <param name="cause">The hangup cause code (Optional)</param>
        /// <remarks>
        /// Common cause codes:
        /// - 16: Normal call clearing (most common)
        /// - 17: User busy
        /// - 19: No answer from user
        /// - 21: Call rejected
        /// 
        /// The cause code affects:
        /// - CDR records
        /// - Billing systems
        /// - Call statistics
        /// - SIP/IAX2 protocol signaling
        /// </remarks>
        public HangupAction(string channel, int cause) : this(channel)
        {
            Cause = cause;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Hangup"</value>
        public override string Action => "Hangup";

        /// <summary>
        /// Gets or sets the channel to hangup.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The channel name to terminate.
        /// </value>
        /// <remarks>
        /// Channel Requirements:
        /// - Must be a valid, active channel
        /// - Format depends on channel technology
        /// - Case-sensitive exact match required
        /// - Must include unique identifier suffix
        /// 
        /// Channel Examples:
        /// - "SIP/1001-00000001" (SIP peer call)
        /// - "IAX2/provider/5551234567-00000001" (IAX trunk call)
        /// - "DAHDI/1-1" (DAHDI channel)
        /// - "Local/1001@from-internal-00000001;1" (Local channel leg 1)
        /// - "Local/1001@from-internal-00000001;2" (Local channel leg 2)
        /// - "PJSIP/1001-00000001" (PJSIP endpoint)
        /// 
        /// The channel must exist and be in an active state.
        /// Invalid or non-existent channels will result in an error response.
        /// </remarks>
        public string Channel { get; set; }

        /// <summary>
        /// Gets or sets the hangup cause code.
        /// This property is optional.
        /// </summary>
        /// <value>
        /// ITU-T Q.850 cause code (default: 16 - Normal call clearing).
        /// </value>
        /// <remarks>
        /// Cause Code Categories:
        /// 
        /// Normal (1-15):
        /// - 16: Normal call clearing
        /// 
        /// Normal with additional info (16-31):
        /// - 17: User busy
        /// - 18: No user responding  
        /// - 19: No answer from user (timeout)
        /// - 20: Subscriber absent
        /// - 21: Call rejected
        /// - 22: Number changed
        /// - 26: Non-selected user clearing
        /// - 27: Destination out of order
        /// - 28: Invalid number format (incomplete)
        /// - 29: Facility rejected
        /// - 31: Normal, unspecified
        /// 
        /// Resource unavailable (34-47):
        /// - 34: No circuit/channel available
        /// - 38: Network out of order
        /// - 41: Temporary failure
        /// - 42: Switching equipment congestion
        /// - 43: Access information discarded
        /// - 44: Requested circuit/channel not available
        /// - 47: Resource unavailable, unspecified
        /// 
        /// Service not available (49-63):
        /// - 50: Requested facility not subscribed
        /// - 57: Bearer capability not authorized
        /// - 58: Bearer capability not presently available
        /// - 63: Service or option not available
        /// 
        /// Service not implemented (65-79):
        /// - 65: Bearer capability not implemented
        /// - 66: Channel type not implemented
        /// - 69: Requested facility not implemented
        /// - 70: Only restricted digital info bearer capability available
        /// - 79: Service or option not implemented
        /// 
        /// Invalid message (81-95):
        /// - 81: Invalid call reference value
        /// - 82: Identified channel does not exist
        /// - 83: Call identity does not exist
        /// - 84: Call identity in use
        /// - 85: No call suspended
        /// - 86: Call having requested call identity cleared
        /// - 87: User not member of CUG
        /// - 88: Incompatible destination
        /// - 91: Invalid transit network selection
        /// - 95: Invalid message, unspecified
        /// 
        /// Protocol error (96-127):
        /// - 96: Mandatory information element is missing
        /// - 97: Message type non-existent/not implemented
        /// - 98: Message not compatible with call state
        /// - 99: Information element non-existent/not implemented
        /// - 100: Invalid information element contents
        /// - 101: Message not compatible with call state
        /// - 102: Recovery on timer expiry
        /// - 103: Parameter non-existent/not implemented
        /// - 110: Message with unrecognized parameter discarded
        /// - 111: Protocol error, unspecified
        /// - 127: Internetworking, unspecified
        /// 
        /// Usage Guidelines:
        /// - Use 16 (Normal clearing) for standard hangups
        /// - Use 17 (User busy) when endpoint is busy
        /// - Use 19 (No answer) for timeout situations
        /// - Use 21 (Call rejected) for security/policy rejections
        /// - Use appropriate codes for billing and statistics accuracy
        /// </remarks>
        public int? Cause { get; set; }
    }
}