using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using Sufficit.Asterisk.Manager.Response;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sufficit.Asterisk.IO;
using static Sufficit.Asterisk.Manager.ManagerResponseBuilder;
using static Sufficit.Asterisk.Manager.ManagerActionBuilder;
using Sufficit.Json;

namespace Sufficit.Asterisk.Manager.Connection
{
    /// <summary>
    /// Responsible for dispatching actions to the Asterisk server and managing the lifecycle
    /// of response handlers. It maps outgoing actions to incoming responses using ActionId.
    /// </summary>
    public class ActionDispatcher : AMISocketManager, IActionDispatcher, IDisposable
    {
        private static readonly ILogger _logger = ManagerLogger.CreateLogger<ActionDispatcher>();
        private readonly ManagerResponseHandlers _handlers;
        private long _actionIdCounter;

        public ActionDispatcher (ManagerConnectionParameters parameters) : base (parameters)
        {
            _handlers = new ManagerResponseHandlers();
        }

        #region SEND ACTION

        public async Task<ManagerResponseEvent> SendActionAsync (ManagerAction action, CancellationToken cancellationToken)
           => await SendActionAsync<ManagerResponseEvent>(action, cancellationToken);

        public async Task<ManagerResponseEvent> SendActionAsync<TAction>(CancellationToken cancellationToken) where TAction : ManagerAction, new()
            => await SendActionAsync<ManagerResponseEvent> (new TAction(), cancellationToken);

        public async Task<TResponse> SendActionAsync<TResponse> (ManagerAction action, CancellationToken cancellationToken) where TResponse : ManagerResponseEvent
        {
            var tcs = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                // Create a handler that will complete the Task when the response arrives.
                var handler = new TaskResponseHandler<TResponse>(action, tcs);
                await SendActionAsync(action, handler, cancellationToken);
                return await tcs.Task;
            }
        }

        /// <inheritdoc cref="IActionDispatcher.SendActionAsync(ManagerAction, IResponseHandler?, CancellationToken)"/>
        public virtual async Task SendActionAsync (ManagerAction action, IResponseHandler? responseHandler, CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();

            if (responseHandler == null)
            {
                string buffer = BuildAction(action, string.Empty);
                await base.WriteAsync(buffer, cancellationToken);
                return;
            }

            string internalActionId = $"{GetHashCode()}_{Interlocked.Increment(ref _actionIdCounter)}";
            action.ActionId ??= Guid.NewGuid().ToString("N").Substring(0, 12);

            responseHandler.Hash = internalActionId.GetHashCode();
            _handlers.Add(responseHandler);

            try
            {
                string buffer = BuildAction(action, internalActionId);
                await base.WriteAsync(buffer, cancellationToken);
            }
            catch
            {
                _handlers.Remove(responseHandler);
                throw;
            }
        }

        protected void ThrowIfNotConnected()
        {
            if (!IsConnected)            
                throw new NotConnectedException("not connected to Asterisk server");            
        }

        #endregion

        /// <summary>
        /// Called by the connection manager when a response packet is received.
        /// Finds the corresponding handler and completes it with the response.
        /// </summary>
        protected void DispatchResponse (IDictionary<string, string> buffer)
        {
            if (buffer == null || buffer.Count == 0)
            {
                _logger.LogWarning("null or empty buffer on DispatchResponse");
                return;
            }

            if (!TryExtractActionId(buffer, out string? AMIActionId))
            {
                var jsonBuffer = buffer.ToJson();

                // this normally happens when the buffer is out of order or malformed by a previous failure
                _logger.LogWarning("can't extract AMI ActionId from buffer: {buffer}", jsonBuffer);
                return;
            }

            try
            {
                var InternalActionId = GetInternalActionId(AMIActionId);
                if (string.IsNullOrWhiteSpace(InternalActionId))
                    throw new ResponseBuildException($"can't get internal action id for AMI Actiond Id: {AMIActionId}");

                var hash = InternalActionId.GetHashCode();
                var handler = _handlers.Pull(hash);
                if (handler == null)
                    throw new ResponseBuildException($"no handler found for response with AMI ActionID '{AMIActionId}'");

                if (handler.ResponseType == null)
                    throw new ResponseBuildException("no response type setted for this handler");

                var response = BuildResponse(handler.ResponseType, AMIActionId, buffer);
                // _logger.LogWarning("manager response type: {type}, content: {json}", response.GetType(), response.ToJson());

                handler.HandleResponse(response);
            }
            catch (AsteriskManagerException ex)
            {
                _logger.LogError(ex, "error on dispatch response for ActionId '{AMIActionId}'", AMIActionId);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "key not found on dispatch response for ActionId '{AMIActionId}'", AMIActionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "unknown error on dispatch response for ActionId '{AMIActionId}'", AMIActionId);
            }
        }

        protected void FailAllHandlers(Exception ex)
        {
            foreach (var handler in _handlers.GetAll())
            {
                try
                {
                    // If one handler fails, it should not prevent others
                    // from being notified, nor should it break the disconnect chain.
                    handler.HandleException(ex);
                }
                catch (Exception handlerEx)
                {
                    _logger.LogError(handlerEx, "A response handler threw an exception while being failed. ActionId: {ActionId}", handler.Action.ActionId);
                }
            }
            _handlers.Clear();
        }

        public override void Dispose()
        {
            FailAllHandlers(new ObjectDisposedException("ActionDispatcher is being disposed."));
            base.Dispose();
        }
    }
}
