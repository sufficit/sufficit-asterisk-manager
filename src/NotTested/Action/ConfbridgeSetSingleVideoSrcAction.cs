using Sufficit.Asterisk.Manager.Action;

namespace Sufficit.Asterisk.Manager.Action
{
    public class ConfbridgeSetSingleVideoSrcAction : ManagerAction
    {
        /// <summary>
        ///     Stops recording a specified conference.
        /// </summary>
        public ConfbridgeSetSingleVideoSrcAction()
        {
        }

        /// <summary>
        ///     Stops recording a specified conference.
        /// </summary>
        /// <param name="conference"></param>
        public ConfbridgeSetSingleVideoSrcAction(string conference, string channel)
        {
            Conference = conference;
            Channel = channel;
        }

        public string Conference { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;

        public override string Action
        {
            get { return "ConfbridgeSetSingleVideoSrc"; }
        }
    }
}
