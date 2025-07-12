using System;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    ///     Lists data about all active conferences. ConfbridgeListRooms will follow as separate events,
    ///     followed by a final event called ConfbridgeListRoomsComplete.
    /// </summary>
    public class ConfbridgeListRoomsAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "ConfbridgeListRooms"</value>
        public override string Action => "ConfbridgeListRooms";

        /// <summary>
        /// Returns the event type that indicates completion of the ConfbridgeListRooms action.
        /// </summary>
        /// <returns>The Type of ConfbridgeListRoomsCompleteEvent</returns>
        public override Type ActionCompleteEventClass()
        {
            return typeof(ConfbridgeListRoomsCompleteEvent);
        }
    }
}