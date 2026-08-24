namespace Sufficit.Asterisk.Manager.Action
{
    public class ConfbridgeUnmuteAction : ManagerAction
    {
        /// <summary>
        ///     Unmutes a specified user in a specified conference.
        /// </summary>
        public ConfbridgeUnmuteAction()
        {
        }

        /// <summary>
        ///     Unmutes a specified user in a specified conference.
        /// </summary>
        /// <param name="conference"></param>
        /// <param name="channel"></param>
        public ConfbridgeUnmuteAction(string conference, string channel)
        {
            Conference = conference;
            Channel = channel;
        }

        public string Conference { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;

        public override string Action
        {
            get { return "ConfbridgeUnmute"; }
        }
    }
}
