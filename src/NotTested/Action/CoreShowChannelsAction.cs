using Sufficit.Asterisk.Manager.Action;

namespace AsterNET.Manager.Action
{
    /// <summary>
    /// Privilege: system,reporting,all
    /// </summary>
    public class CoreShowChannelsAction : ManagerAction
    {
        public override string Action
        {
            get { return "CoreShowChannels"; }
        }
    }
}