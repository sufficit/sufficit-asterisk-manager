namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The CoreShowChannels action displays detailed information about all active channels.
    /// This action provides comprehensive channel information including states, contexts, extensions, and more.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: CoreShowChannels
    /// Purpose: Display active channels and their current state
    /// Privilege Required: system,reporting,all
    /// 
    /// Usage scenarios:
    /// - Monitoring active calls
    /// - Channel diagnostics and troubleshooting
    /// - Call center reporting
    /// - System health monitoring
    /// 
    /// Response Events:
    /// - CoreShowChannel: One event per active channel
    /// - CoreShowChannelsComplete: Indicates end of channel list
    /// 
    /// Asterisk Versions: 
    /// - Available since Asterisk 1.6
    /// - Enhanced in later versions with additional channel information
    /// </remarks>
    public class CoreShowChannelsAction : ManagerAction
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "CoreShowChannels"</value>
        public override string Action => "CoreShowChannels";
    }
}