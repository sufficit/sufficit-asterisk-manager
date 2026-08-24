namespace Sufficit.Asterisk.Manager.Action
{
    public class ConfbridgeKickAction : ManagerAction
    {
        /// <summary>
        ///     Removes a specified user from a specified conference.
        /// </summary>
        public ConfbridgeKickAction()
        {
        }

        /// <summary>
        ///     Removes a specified user from a specified conference.
        /// </summary>
        /// <param name="conference"></param>
        /// <param name="channel"></param>
        public ConfbridgeKickAction(string conference, string channel)
        {
            Conference = conference;
            Channel = channel;
        }

        public string Conference { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;

        public override string Action
        {
            get { return "ConfbridgeKick"; }
        }
    }
}
