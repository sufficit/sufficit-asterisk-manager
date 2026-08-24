namespace Sufficit.Asterisk.Manager.Action
{
    public class QueueLogAction : ManagerAction
    {
        /// <summary>
        ///     Adds custom entry in queue_log.
        /// </summary>
        public QueueLogAction()
        {
        }

        /// <summary>
        ///     Adds custom entry in queue_log.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="event"></param>
        /// <param name="uniqueid"></param>
        /// <param name="interface"></param>
        /// <param name="message"></param>
        public QueueLogAction(string queue, string @event, string uniqueid, string @interface, string message)
        {
            Queue = queue;
            Event = @event;
            Uniqueid = uniqueid;
            Interface = @interface;
            Message = message;
        }

        public override string Action
        {
            get { return "QueueLog"; }
        }

        public string Queue { get; set; } = string.Empty;

        public string Event { get; set; } = string.Empty;

        public string Uniqueid { get; set; } = string.Empty;

        public string Interface { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}
