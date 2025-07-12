using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Action;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sufficit.Asterisk.Manager
{
    public class ManagerResponseHandlers
    {
        private static ILogger _logger = ManagerLogger.CreateLogger(typeof(ManagerResponseHandlers));

        public IDictionary<int, IResponseHandler> Actions { get; }
        public IDictionary<int, IResponseHandler> Ping { get; }
        public IDictionary<int, IResponseHandler> Events { get; }

        public ManagerResponseHandlers() 
        {
            Actions = new ConcurrentDictionary<int, IResponseHandler>();
            Ping = new ConcurrentDictionary<int, IResponseHandler>();
            Events = new ConcurrentDictionary<int, IResponseHandler>();
        }

        /// <summary>
        /// Retrieves and removes the response handler associated with the specified hash.
        /// </summary>
        /// <remarks>This method searches for the handler in two collections: response handlers and ping
        /// handlers. If a matching handler is found, it is removed from its respective collection before being
        /// returned.</remarks>
        /// <param name="hash">The hash value used to identify the response handler.</param>
        /// <returns>The response handler associated with the specified hash, or <see langword="null"/> if no handler is found.</returns>
        public IResponseHandler? Pull (int hash)
        {
            if (Actions.TryGetValue(hash, out var handler))
            {
                Actions.Remove(hash);
                return handler;
            }
            if (Ping.TryGetValue(hash, out handler))
            {
                Ping.Remove(hash);
                return handler;
            }            
            return null;
        }

        /// <summary>
        /// Adds a response handler to the appropriate collection based on its action type.
        /// </summary>
        /// <remarks>If the handler's hash value is zero, the method does nothing. Handlers with a <see
        /// cref="PingAction"/> are added to the ping handlers collection, while other handlers are added to the general
        /// response handlers collection. This method is thread-safe and ensures proper synchronization when modifying
        /// the handler collections.</remarks>
        /// <param name="handler">The response handler to add. Must have a non-zero hash value.</param>
        public void Add (IResponseHandler handler)
        {
            if (handler.Hash == 0) return;
            
            if (handler.Action is PingAction)
                Ping[handler.Hash] = handler;
            else
                Actions[handler.Hash] = handler;

            _logger.LogTrace("total handlers, actions: {actions}, ping: {ping}, events: {events}", Actions.Count, Ping.Count, Events.Count);
        }

        /// <summary>
        /// Removes the specified response handler from the collection of active handlers.
        /// </summary>
        /// <remarks>If the <paramref name="handler"/> has a hash value of zero, the method does nothing.
        /// This method is thread-safe and ensures proper synchronization when modifying the collection of
        /// handlers.</remarks>
        /// <param name="handler">The response handler to remove. The handler must have a non-zero hash value.</param>
        public bool Remove (IResponseHandler handler)
        {
            if (handler.Hash == 0) return false;

            if (handler.Action is PingAction)
                return Ping.Remove(handler.Hash);
            else
                return Actions.Remove(handler.Hash);            
        }

        /// <summary>
        /// Adds a response event handler to the collection of handlers.
        /// </summary>
        /// <remarks>If the <paramref name="handler"/> has a <see cref="IResponseHandler.Hash"/> value of
        /// 0, the method does nothing. Otherwise, the handler is added to the collection, replacing any existing
        /// handler with the same hash. This method is thread-safe.</remarks>
        /// <param name="handler">The response handler to add. The <see cref="IResponseHandler.Hash"/> property must be non-zero.</param>
        public void AddUserEvent (IResponseHandler handler)
        {
            if (handler.Hash == 0) return;
            Events[handler.Hash] = handler;            
        }

        /// <summary>
        /// Removes the specified response event handler from the collection of handlers.
        /// </summary>
        /// <remarks>If the <paramref name="handler"/> has a hash value of zero, the method does nothing.
        /// This method is thread-safe and ensures that the handler is removed from the collection in a synchronized
        /// manner.</remarks>
        /// <param name="handler">The response event handler to remove. The handler must have a non-zero hash value.</param>
        public bool RemoveUserEvent(IResponseHandler handler)
        {
            if (handler.Hash == 0) return false;
            return Events.Remove(handler.Hash);
        }

        public IEnumerable<IResponseHandler> GetAll()
            => Actions.Values.Concat(Ping.Values).Concat(Events.Values).ToList();

        public void Clear()
        {
            Actions.Clear();
            Ping.Clear();
            Events.Clear();
        }
    }
}
