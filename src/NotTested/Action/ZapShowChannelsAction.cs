using System;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events;

namespace AsterNET.Manager.Action
{
    /// <summary>
    /// The ZapShowChannelsAction requests the state of all zap channels.
    /// For each zap channel a ZapShowChannelsEvent is generated. After all zap
    /// channels have been listed a ZapShowChannelsCompleteEvent is generated.
    /// </summary>
    /// <seealso cref="ZapShowChannelsEvent" />
    /// <seealso cref="ZapShowChannelsCompleteEvent" />
    public class ZapShowChannelsAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "ZapShowChannels"</value>
        public override string Action => "ZapShowChannels";

        /// <summary>
        /// Returns the event type that indicates completion of the ZapShowChannels action.
        /// </summary>
        /// <returns>The Type of ZapShowChannelsCompleteEvent</returns>
        public override Type ActionCompleteEventClass()
        {
            return typeof(ZapShowChannelsCompleteEvent);
        }
    }
}