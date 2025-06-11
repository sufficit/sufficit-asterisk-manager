using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager.Connection
{
    public interface IActionDispatcher
    {        
        Task<TResponse> SendActionAsync<TResponse>(ManagerAction action, CancellationToken cancellationToken) where TResponse : ManagerResponseEvent;

        /// <summary>
        /// Sends a ManagerAction with a callback handler for processing the response.
        /// This is generally used for more complex or fire-and-forget scenarios.
        /// </summary>
        /// <param name="action">The action to send.</param>
        /// <param name="responseHandler">An optional handler to process the response.</param>
        /// <returns>An internal hash code for the handler, or 0 if no handler is provided.</returns>
        void SendAction(ManagerAction action, IResponseHandler? responseHandler);
    }
}
