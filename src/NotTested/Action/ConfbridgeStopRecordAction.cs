﻿using Sufficit.Asterisk.Manager.Action;

namespace Sufficit.Asterisk.Manager.Action
{
    public class ConfbridgeStopRecordAction : ManagerAction
    {
        /// <summary>
        ///     Stops recording a specified conference.
        /// </summary>
        public ConfbridgeStopRecordAction()
        {
        }

        /// <summary>
        ///     Stops recording a specified conference.
        /// </summary>
        /// <param name="conference"></param>
        public ConfbridgeStopRecordAction(string conference)
        {
            Conference = conference;
        }

        public string Conference { get; set; }

        public override string Action
        {
            get { return "ConfbridgeStopRecord"; }
        }
    }
}