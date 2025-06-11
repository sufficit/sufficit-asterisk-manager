using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using Sufficit.Asterisk.Manager.Response;
using System;
using System.Threading;

namespace Sufficit.Asterisk.Manager // Corrected namespace
{
    /// <summary>
    /// A simple response handler that stores the received response.
    /// Used by synchronous SendAction methods in ManagerConnection that utilize an AutoResetEvent.
    /// </summary>
    public class ResponseHandler : IResponseHandler
    {
        private AutoResetEvent? _autoEvent; // Nullable, as it's cleared in Free()
        private int _hash;
        private ManagerResponseEvent? _response; 

        /// <summary>
        /// Creates a new <see cref="ResponseHandler"/>.
        /// </summary>
        /// <param name="action">The action being sent. Must be from Sufficit.Asterisk.Manager.Action.</param>
        /// <param name="autoEvent">The AutoResetEvent to signal upon completion, error, or cancellation.</param>
        public ResponseHandler (ManagerAction action, AutoResetEvent? autoEvent)
        {
            Action = action;
            this._autoEvent = autoEvent; // autoEvent can be null if just storing response without signaling
            this._response = null;
        }

        /// <summary>
        /// Gets the received response.
        /// </summary>
        public ManagerResponseEvent? Response => this._response;

        /// <summary>
        /// Gets the action this handler is for. Implements IResponseHandler.
        /// </summary>
        public ManagerAction Action { get; }

        /// <inheritdoc cref="IResponseHandler.ResponseType"/> 
        public Type? ResponseType { get; set; }

        /// <summary>
        /// Gets or sets the hash code for this handler. Implements IResponseHandler.
        /// </summary>
        public int Hash
        {
            get => _hash;
            set => _hash = value;
        }

        /// <summary>
        /// Clears internal references. Can be called to help with resource cleanup if needed.
        /// </summary>
        public void Free()
        {
            // _logger?.LogTrace("ResponseHandler (Hash: {HandlerHash}) Free() called for action {ActionName}.", this.Hash, this._action?.Action);
            _autoEvent = null;
            _response = null;
            // _action is readonly, cannot be nulled after construction.
        }

        /// <summary>
        /// This method is called when a response is received. Implements IResponseHandler.
        /// </summary>
        /// <param name="response">The response received. Type from Sufficit.Asterisk.Manager.Response.</param>
        public virtual void HandleResponse(ManagerResponseEvent response)
        {
            // _logger?.LogDebug("ResponseHandler (Hash: {HandlerHash}) HandleResponse: Received {ResponseType} for action {ActionName}.", this.Hash, response?.GetType().Name, this._action?.Action);
            this._response = response;
            _autoEvent?.Set(); // Signal that a response (success or error) was processed
        }

        /// <summary>
        /// Handles an exception. Implements IResponseHandler.
        /// This might be called if ManagerConnection forces pending handlers to terminate (e.g., on disconnect).
        /// </summary>
        /// <param name="ex">The exception that occurred.</param>
        public void HandleException(Exception ex)
        {
            // _logger?.LogError(ex, "ResponseHandler (Hash: {HandlerHash}) handling exception for action {ActionName}.", this.Hash, _action?.Action);

            // Ensure a response is set, marking it as an error.
            if (this._response == null) // Only set if no other response (e.g. timeout ManagerError) has been set.
            {
                this._response = new ManagerError(ex.Message, ex); // Uses your ManagerError constructor
            }
            _autoEvent?.Set(); // Signal to unblock the waiting thread
        }

        /// <summary>
        /// Handles a cancellation request. Implements IResponseHandler.
        /// For synchronous handlers, this usually means an operation was aborted externally.
        /// </summary>
        /// <param name="cancellationToken">The token that signaled cancellation.</param>
        public void HandleCancel(CancellationToken cancellationToken)
        {
            // _logger?.LogWarning("ResponseHandler (Hash: {HandlerHash}) handling cancellation for action {ActionName}.", this.Hash, _action?.Action);
            if (this._response == null)
            {
                this._response = new ManagerError("Operation was canceled.", new OperationCanceledException(cancellationToken));
            }
            _autoEvent?.Set(); // Signal to unblock the waiting thread
        }
    }
}