using Sufficit.Asterisk.Manager.Events;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using Sufficit.Asterisk.Manager.Response;
using System.Collections;
using System.Collections.Generic;

namespace Sufficit.Asterisk.Manager
{
    /// <summary>
    ///     Collection of ResponseEvent. Use in events generation actions.
    /// </summary>
    public class ResponseEvents
    {
        private readonly List<IResponseEvent> events;

        /// <summary>
        ///     Creates a new <see cref="ResponseEvents"/>.
        /// </summary>
        public ResponseEvents()
        {
            events = new List<IResponseEvent>();
        }

        /// <summary>
        ///     Gets or sets the response.
        /// </summary>
        public ManagerResponseEvent? Response { get; set; }

        /// <summary>
        ///     Gets the list of events.
        /// </summary>
        public List<IResponseEvent> Events { get { lock (((IList)events).SyncRoot) return events; } }

        /// <summary>
        ///     Indicates if all events have been received.
        /// </summary>
        public bool Complete { get; set; }

        /// <summary>
        ///     Adds a ResponseEvent that has been received.
        /// </summary>
        /// <param name="e"><see cref="IResponseEvent"/></param>
        public void AddEvent(IResponseEvent e)
        {
            lock (((IList) events).SyncRoot)
            {
                events.Add(e);
            }
        }
    }
}