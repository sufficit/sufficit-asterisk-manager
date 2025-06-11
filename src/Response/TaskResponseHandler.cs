using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager.Response
{
    public class TaskResponseHandler<TResponse> : IResponseHandler where TResponse : ManagerResponseEvent
    {
        private readonly TaskCompletionSource<TResponse> _tcs;

        /// <inheritdoc cref="IResponseHandler.Hash"/> 
        public int Hash { get; set; }

        /// <inheritdoc cref="IResponseHandler.ResponseType"/> 
        public Type? ResponseType { get; set; }

        /// <inheritdoc cref="IResponseHandler.Action"/>
        public ManagerAction Action { get; }

        public TaskResponseHandler(ManagerAction action, TaskCompletionSource<TResponse> tcs)
        {
            Action = action;
            _tcs = tcs;
            ResponseType = typeof(TResponse);
        }

        /// <inheritdoc cref="IResponseHandler.HandleResponse"/>
        public void HandleResponse (ManagerResponseEvent response)
        {
            if (response is TResponse typedResponse)
            {
                if (typedResponse.Exception != null)
                {
                    var exception = new AsteriskManagerResponseFailedException(
                        "the manager response indicates a failure.",
                        Action,
                        typedResponse.Exception
                    );
                    _tcs.TrySetException(exception);
                }
                else
                {
                    _tcs.TrySetResult(typedResponse);
                }
            }
            else
            {
                var invalid = new InvalidCastException($"response type mismatch for action {Action.Action}. expected {typeof(TResponse).FullName} but got {response?.GetType().FullName}");
                var exception = new AsteriskManagerResponseFailedException(                        
                        Action,
                        invalid
                    );
                _tcs.TrySetException(exception);
            }
        }

        /// <inheritdoc cref="IResponseHandler.HandleException"/>
        public void HandleException(Exception exception)
            => _tcs.TrySetException(exception);        

        /// <inheritdoc cref="IResponseHandler.HandleCancel"/>
        public void HandleCancel(CancellationToken cancellationToken)
            => _tcs.TrySetCanceled(cancellationToken);        
    }
}