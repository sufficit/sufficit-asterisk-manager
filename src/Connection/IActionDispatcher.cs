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
        /// <summary>
        /// Sends a specified manager action to the server and asynchronously waits for a response.
        /// </summary>
        /// <remarks>This method sends the specified action to the server and waits for a response of the
        /// specified type. Ensure that the <typeparamref name="TResponse"/> type matches the expected response for the
        /// given action.</remarks>
        /// <typeparam name="TResponse">The type of the response expected from the server. Must derive from <see cref="ManagerResponseEvent"/>.</typeparam>
        /// <param name="action">The manager action to be sent. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If the operation is canceled, the task will be canceled.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response of type
        /// <typeparamref name="TResponse"/>.</returns>
        Task<TResponse> SendActionAsync<TResponse>(ManagerAction action, CancellationToken cancellationToken) where TResponse : ManagerResponseEvent;

        /// <summary>
        /// Sends the specified action to the manager asynchronously and optionally handles the response.
        /// </summary>
        /// <param name="action">The action to be sent to the manager. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="responseHandler">An optional handler for processing the response. If <see langword="null"/>, the response will not be
        /// handled.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. Passing a canceled token will immediately cancel the request operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task completes when the action is sent and the
        /// response (if any) is processed.</returns>
        Task SendActionAsync(ManagerAction action, IResponseHandler? responseHandler, CancellationToken cancellationToken);
    }
}
