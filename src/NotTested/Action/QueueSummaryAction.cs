using System;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The QueueSummaryAction requests summary information about all queues.
    /// For each queue a QueueSummaryEvent is generated. After all summary information
    /// has been reported a QueueSummaryCompleteEvent is generated.
    /// Available since Asterisk 1.4
    /// </summary>
    /// <seealso cref="QueueSummaryEvent" />
    /// <seealso cref="QueueSummaryCompleteEvent" />
    public class QueueSummaryAction : ManagerActionEvent
    {
        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "QueueSummary"</value>
        public override string Action => "QueueSummary";

        /// <summary>
        /// Gets or sets the queue name filter.
        /// When specified, only information about the named queue is returned.
        /// </summary>
        public string? Queue { get; set; }

        /// <summary>
        /// Returns the event type that indicates completion of the QueueSummary action.
        /// </summary>
        /// <returns>The Type of QueueSummaryCompleteEvent</returns>
        public override Type ActionCompleteEventClass()
        {
            return typeof(QueueSummaryCompleteEvent);
        }
    }
}
