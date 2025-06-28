using System;
using System.Threading;
using Sufficit.Asterisk.Manager.Action; // Para ManagerAction, ManagerActionEvent
using Sufficit.Asterisk.Manager.Events; // Para IManagerEvent, IResponseEvent
using Sufficit.Asterisk.Manager.Response; // MUITO IMPORTANTE: Para ManagerResponse, ManagerError
using Microsoft.Extensions.Logging;
using AsterNET.Manager.Action;
using Sufficit.Asterisk.Manager.Events.Abstracts;

namespace Sufficit.Asterisk.Manager // Ou o namespace onde esta classe realmente reside
{
    /// <summary>
    /// A combined event and response handler that adds received events and the response 
    /// to a ResponseEvents object. Used by synchronous methods like SendEventGeneratingAction.
    /// </summary>
    public class ResponseEventHandler : IResponseHandler
    {
        private static ILogger _logger = ManagerLogger.CreateLogger<ResponseEventHandler>();

        private ManagerActionEvent action;
        private AutoResetEvent? autoEvent;
        private ResponseEvents events;
        private int hash;

        /// <summary>
        /// Creates a new instance of <see cref="ResponseEventHandler"/>.
        /// </summary>
        public ResponseEventHandler(
            ManagerActionEvent action,
            AutoResetEvent autoEvent)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.autoEvent = autoEvent ?? throw new ArgumentNullException(nameof(autoEvent));
            this.events = new ResponseEvents();
        }

        /// <summary>
        /// Gets the action this handler is for. Implements IResponseHandler.
        /// </summary>
        ManagerAction IResponseHandler.Action => this.action;

        /// <inheritdoc cref="IResponseHandler.ResponseType"/> 
        public Type? ResponseType { get; set; }

        /// <summary>
        /// Gets or sets the hash code for this handler. Implements IResponseHandler.
        /// </summary>
        public int Hash
        {
            get => hash;
            set => hash = value;
        }

        /// <summary>
        /// Clears internal references and data. Called after the handler is no longer needed.
        /// </summary>
        public void Free()
        {
            _logger.LogTrace("ResponseEventHandler (Hash: {HandlerHash}) Free() called for action {ActionName}.", this.Hash, this.action?.Action);           
            autoEvent = null;
            // action = null; // ManagerActionEvent não é nullable aqui.
            if (events != null)
            {
                events.Events.Clear();
                events.Response = null;
                events.Complete = false;
            }
        }

        /// <summary>
        /// This method is called when a response is received. Implements IResponseHandler.
        /// </summary>
        public void HandleResponse (ManagerResponseEvent response) // Tipo ManagerResponse deve ser resolvido
        {
            if (events == null || autoEvent == null)
            {
                _logger.LogWarning("HandleResponse called on a freed or improperly initialized ResponseEventHandler (Hash: {HandlerHash}). Response: {ResponseType}", this.Hash, response.GetType().Name);
                return;
            }

            _logger.LogDebug("ResponseEventHandler (Hash: {HandlerHash}) HandleResponse: Received {ResponseType} for action {ActionName}.", this.Hash, response.GetType().Name, this.action?.Action);
            events.Response = response;
            if (response is ManagerError) // Tipo ManagerError deve ser resolvido
            {
                _logger.LogWarning("ResponseEventHandler (Hash: {HandlerHash}) received ManagerError for action {ActionName}. Marking as complete. Message: {ErrorMessage}", this.Hash, this.action?.Action, response.Message);
                events.Complete = true;
            }

            if (events.Complete)
            {
                _logger.LogDebug("ResponseEventHandler (Hash: {HandlerHash}) is complete (due to response). Signaling AutoResetEvent.", this.Hash);
                autoEvent.Set();
            }
        }

        /// <summary>
        /// Handles an event received from the Asterisk server.
        /// </summary>
        public void HandleEvent(IManagerEvent e) // Tipo IManagerEvent deve ser resolvido
        {
            if (events == null || autoEvent == null || action == null)
            {
                _logger.LogWarning("HandleEvent called on a freed or improperly initialized ResponseEventHandler (Hash: {HandlerHash}). Event: {EventType}", this.Hash, e?.GetType().Name);
                return;
            }

            _logger.LogDebug("ResponseEventHandler (Hash: {HandlerHash}) HandleEvent: Received {EventType} for action {ActionName}.", this.Hash, e?.GetType().Name, this.action.Action);
            if (e is IResponseEvent responseEvent) // Tipo IResponseEvent deve ser resolvido
            {
                events.AddEvent(responseEvent);
            }

            Type? actionCompleteType = null;
            try
            {
                actionCompleteType = action.ActionCompleteEventClass();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResponseEventHandler (Hash: {HandlerHash}): Error getting ActionCompleteEventClass from action {ActionName}", this.Hash, action.Action);
                HandleException(new AsteriskManagerException($"Failed to get ActionCompleteEventClass for action {action.Action}", ex));
                return;
            }

            if (actionCompleteType != null && actionCompleteType.IsInstanceOfType(e))
            {
                _logger.LogInformation("ResponseEventHandler (Hash: {HandlerHash}): ActionCompleteEvent ({EventTypeName}) received for action {ActionName}. Marking as complete.", this.Hash, e.GetType().Name, action.Action);
                lock (events)
                {
                    events.Complete = true;
                }

                if (events.Response != null)
                {
                    _logger.LogDebug("ResponseEventHandler (Hash: {HandlerHash}) is complete (due to event) and response already received. Signaling AutoResetEvent.", this.Hash);
                    autoEvent.Set();
                }
                else
                {
                    _logger.LogDebug("ResponseEventHandler (Hash: {HandlerHash}) action complete event received, but awaiting initial response before signaling.", this.Hash);
                }
            }
        }

        /// <summary>
        /// Handles an exception. Implements IResponseHandler.
        /// </summary>
        public void HandleException(Exception ex)
        {
            if (events == null || autoEvent == null)
            {
                _logger.LogWarning("HandleException called on a freed or improperly initialized ResponseEventHandler (Hash: {HandlerHash}). Exception: {ExceptionMessage}", this.Hash, ex?.Message);
                return;
            }

            _logger.LogError(ex, "ResponseEventHandler (Hash: {HandlerHash}) handling exception for action {ActionName}.", this.Hash, action?.Action);

            if (events.Response == null || !(events.Response is ManagerError))
            {
                // CORRIGIDO: Usa o construtor de ManagerError que aceita mensagem e Exception.
                events.Response = new ManagerError(ex.Message, ex);
            }
            events.Complete = true;
            autoEvent.Set();
        }

        /// <summary>
        /// Handles a cancellation request. Implements IResponseHandler.
        /// </summary>
        public void HandleCancel(CancellationToken cancellationToken)
        {
            if (events == null || autoEvent == null)
            {
                _logger.LogWarning("HandleCancel called on a freed or improperly initialized ResponseEventHandler (Hash: {HandlerHash}).", this.Hash);
                return;
            }

            _logger.LogWarning("ResponseEventHandler (Hash: {HandlerHash}) handling cancellation for action {ActionName}.", this.Hash, action?.Action);
            if (events.Response == null || !(events.Response is ManagerError))
            {
                // CORRIGIDO: Usa o construtor de ManagerError que aceita mensagem e Exception.
                events.Response = new ManagerError("Operation was canceled by request.", new OperationCanceledException(cancellationToken));
            }
            events.Complete = true;
            autoEvent.Set();
        }
    }
}