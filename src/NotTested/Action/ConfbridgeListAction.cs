﻿using System;
using Sufficit.Asterisk.Manager.Events;

namespace AsterNET.Manager.Action
{
    /*
        https://wiki.asterisk.org/wiki/display/AST/ConfBridge+10#ConfBridge10-ConfBridgeAsteriskManagerInterface%28AMI%29Events
        Action: ConfbridgeList
        Conference: 1111
    */

    /// <summary>
    ///     Lists all users in a particular ConfBridge conference. ConfbridgeList will follow as separate events,
    ///     followed by a final event called ConfbridgeListComplete
    /// </summary>
    public class ConfbridgeListAction : ManagerActionEvent
    {
        /// <summary>
        ///     Lists all users in a particular ConfBridge conference. ConfbridgeList will follow as separate events,
        ///     followed by a final event called ConfbridgeListComplete
        /// </summary>
        /// <param name="conference"></param>
        public ConfbridgeListAction(string conference)
        {
            Conference = conference;
        }

        public string Conference { get; set; }

        public override string Action
        {
            get { return "ConfbridgeList"; }
        }

        public override Type ActionCompleteEventClass()
        {
            return typeof (ConfbridgeListCompleteEvent);
        }
    }
}