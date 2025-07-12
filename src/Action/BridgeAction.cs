using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The BridgeAction bridges (connects) two active channels together.
    /// This action allows two separate calls to be connected so they can communicate directly.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Bridge
    /// Purpose: Connect two active channels for bidirectional audio
    /// Privilege Required: call,all
    /// 
    /// Required Parameters:
    /// - Channel1: First channel to bridge (Required)
    /// - Channel2: Second channel to bridge (Required)
    /// 
    /// Optional Parameters:
    /// - Tone: Play tone during bridge (Optional)
    /// 
    /// Bridge Behavior:
    /// - Creates bidirectional audio path between channels
    /// - Both channels must be active and not already bridged
    /// - Bridge persists until one channel hangs up or is unbridged
    /// - Audio flows directly between channels (no transcoding if same codec)
    /// - Features like call recording may be affected
    /// 
    /// Tone Options (if supported):
    /// - "yes" or "true": Play confirmation tone
    /// - "no" or "false": No tone (default)
    /// - Some versions may support specific tone types
    /// 
    /// Usage Scenarios:
    /// - Call transfer completion
    /// - Conference call creation
    /// - Call supervision and monitoring
    /// - Customer service call routing
    /// - Interactive voice response (IVR) connections
    /// - Call center agent connections
    /// 
    /// Bridge States:
    /// - Before bridge: Channels are separate
    /// - During bridge: Audio flows bidirectionally
    /// - After hangup: Bridge automatically destroyed
    /// 
    /// Asterisk Versions:
    /// - Available since early Asterisk versions
    /// - Bridge framework enhanced in Asterisk 12+
    /// - Tone parameter availability varies by version
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// Uses Asterisk's bridging framework for optimal performance.
    /// May trigger Bridge events for monitoring.
    /// 
    /// Security Considerations:
    /// - Requires appropriate manager privileges
    /// - Can connect any two active channels
    /// - Should be restricted to authorized users
    /// - Consider privacy implications of bridging
    /// 
    /// Error Conditions:
    /// - Channel not found
    /// - Channel already bridged
    /// - Channel not in compatible state
    /// - Insufficient privileges
    /// 
    /// Example Usage:
    /// <code>
    /// // Basic bridge
    /// var bridge = new BridgeAction("SIP/1001-00000001", "SIP/1002-00000002");
    /// 
    /// // Bridge with tone
    /// var bridge = new BridgeAction("SIP/1001-00000001", "SIP/1002-00000002", true);
    /// </code>
    /// </remarks>
    /// <seealso cref="HangupAction"/>
    /// <seealso cref="OriginateAction"/>
    /// <seealso cref="RedirectAction"/>
    public class BridgeAction : ManagerAction
    {
        /// <summary>
        /// Creates a new BridgeAction to connect two channels.
        /// </summary>
        /// <param name="channel1">First channel to bridge (Required)</param>
        /// <param name="channel2">Second channel to bridge (Required)</param>
        /// <remarks>
        /// Channel Requirements:
        /// - Both channels must be active and not bridged
        /// - Channels must be in compatible states (typically Up)
        /// - Channel names must be exact matches including unique identifiers
        /// 
        /// Channel format examples:
        /// - "SIP/1001-00000001" (SIP channel)
        /// - "IAX2/provider-00000001" (IAX2 channel)
        /// - "DAHDI/1-1" (DAHDI channel)
        /// - "Local/1001@from-internal-00000001;1" (Local channel)
        /// - "PJSIP/1001-00000001" (PJSIP channel)
        /// </remarks>
        public BridgeAction(string channel1, string channel2)
        {
            Channel1 = channel1 ?? throw new ArgumentNullException(nameof(channel1));
            Channel2 = channel2 ?? throw new ArgumentNullException(nameof(channel2));
        }

        /// <summary>
        /// Creates a new BridgeAction to connect two channels with optional tone.
        /// </summary>
        /// <param name="channel1">First channel to bridge (Required)</param>
        /// <param name="channel2">Second channel to bridge (Required)</param>
        /// <param name="tone">Whether to play a tone during bridge (Optional)</param>
        /// <remarks>
        /// The tone parameter, when supported, can provide audio feedback
        /// to indicate successful bridge establishment. Not all Asterisk
        /// versions or configurations support this feature.
        /// </remarks>
        public BridgeAction(string channel1, string channel2, bool tone) : this(channel1, channel2)
        {
            Tone = tone;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Bridge"</value>
        public override string Action => "Bridge";

        /// <summary>
        /// Gets or sets the first channel to bridge.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The first channel name to connect.
        /// </value>
        /// <remarks>
        /// Channel1 Requirements:
        /// - Must be a valid, active channel
        /// - Should not already be bridged to another channel
        /// - Must be in a compatible state (typically "Up")
        /// - Format depends on channel technology
        /// 
        /// Compatible Channel States:
        /// - "Up": Channel is answered and ready for audio
        /// - Other states may work depending on channel technology
        /// 
        /// Channel Examples:
        /// - "SIP/1001-00000001" (SIP peer call)
        /// - "IAX2/provider/5551234567-00000001" (IAX trunk call)
        /// - "DAHDI/1-1" (DAHDI channel)
        /// - "Local/1001@from-internal-00000001;1" (Local channel leg)
        /// - "PJSIP/1001-00000001" (PJSIP endpoint)
        /// 
        /// The channel must exist and be in an appropriate state for bridging.
        /// Use Status action or channel events to verify channel state before bridging.
        /// </remarks>
        public string Channel1 { get; set; }

        /// <summary>
        /// Gets or sets the second channel to bridge.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The second channel name to connect.
        /// </value>
        /// <remarks>
        /// Channel2 Requirements:
        /// - Must be a valid, active channel
        /// - Should not already be bridged to another channel
        /// - Must be in a compatible state (typically "Up")
        /// - Should be different from Channel1
        /// 
        /// Bridge Compatibility:
        /// - Both channels should have compatible codecs for optimal performance
        /// - Different channel technologies can be bridged (e.g., SIP to DAHDI)
        /// - Asterisk will handle transcoding if necessary
        /// - Media capabilities are negotiated automatically
        /// 
        /// Performance Considerations:
        /// - Same codec: Direct media path (optimal)
        /// - Different codecs: Transcoding required (CPU intensive)
        /// - Same technology: Often more efficient
        /// - Cross-technology bridging: Additional overhead
        /// 
        /// The second channel must exist and be in an appropriate state for bridging.
        /// Both channels will be connected bidirectionally for audio exchange.
        /// </remarks>
        public string Channel2 { get; set; }

        /// <summary>
        /// Gets or sets whether to play a tone during bridge establishment.
        /// This property is optional.
        /// </summary>
        /// <value>
        /// True to play tone, false for silent bridge (default: false).
        /// </value>
        /// <remarks>
        /// Tone Functionality:
        /// - Provides audio feedback when bridge is established
        /// - Not supported in all Asterisk versions
        /// - May depend on channel technology capabilities
        /// - Typically a brief confirmation beep
        /// 
        /// Tone Behavior:
        /// - Played to one or both channels during bridge
        /// - Duration and frequency may vary by implementation
        /// - Does not affect bridge functionality
        /// - Useful for user interface feedback
        /// 
        /// Version Compatibility:
        /// - Older Asterisk versions may ignore this parameter
        /// - Modern versions typically support tone indication
        /// - Behavior may vary based on Asterisk configuration
        /// 
        /// Alternative Implementations:
        /// If tone is not supported natively, consider:
        /// - Playing announcement before bridge
        /// - Using Playback action on channels
        /// - Custom dialplan logic for audio feedback
        /// </remarks>
        public bool? Tone { get; set; }
    }
}