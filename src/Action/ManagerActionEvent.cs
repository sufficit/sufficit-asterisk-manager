using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// Base class for Manager Actions that return their results through a series of events
    /// rather than a single response. These actions typically generate multiple events
    /// followed by a completion event.
    /// </summary>
    /// <remarks>
    /// ManagerActionEvent is designed for actions that:
    /// - Generate multiple response events
    /// - Have a specific completion event type
    /// - Require event-based result processing
    /// 
    /// Examples include:
    /// - QueueStatus: Returns multiple QueueMember/QueueEntry events + QueueStatusComplete
    /// - Status: Returns multiple Status events + StatusComplete  
    /// - CoreShowChannels: Returns multiple CoreShowChannel events + CoreShowChannelsComplete
    /// 
    /// The completion event type is specified by implementing ActionCompleteEventClass().
    /// This allows the AMI framework to properly detect when all response events have been received.
    /// </remarks>
    public abstract class ManagerActionEvent : ManagerAction
    {
        /// <summary>
        /// Returns the Type of the event that indicates Asterisk has finished 
        /// sending response events for this action.
        /// </summary>
        /// <returns>
        /// The Type of the completion event that signals the end of the event series.
        /// </returns>
        /// <remarks>
        /// This method is used by the AMI framework to determine when all response events
        /// for this action have been received. The completion event serves as a marker
        /// that no more related events will be sent.
        /// 
        /// Examples:
        /// - QueueStatusAction returns typeof(QueueStatusCompleteEvent)
        /// - StatusAction returns typeof(StatusCompleteEvent)
        /// - CoreShowChannelsAction returns typeof(CoreShowChannelsCompleteEvent)
        /// </remarks>
        public abstract Type ActionCompleteEventClass();
    }
}