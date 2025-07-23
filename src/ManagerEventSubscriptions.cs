using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager
{
    /// <summary>
    /// Central event subscription management system for Asterisk Manager Interface (AMI) events.
    /// Coordinates multiple event subscriptions and handles event dispatching using a modern,
    /// high-performance, non-blocking producer-consumer pattern specifically for AMI events.
    /// </summary>
    /// <remarks>
    /// Clean Design:
    /// - ManagerConnection always creates its own internal instance
    /// - External instances can be optionally used via Use() method
    /// - Internal instance is always disposed with the connection
    /// - External instances are never disposed by the connection
    /// 
    /// Event Building:
    /// - Event building logic has been moved to ManagerEventBuilder static class
    /// - This class focuses solely on subscription management and event dispatching
    /// </remarks>
    public class ManagerEventSubscriptions : IManagerEventSubscriptions, IAsyncDisposable
    {
        #region Static Fields and Logger

        private static readonly ILogger _logger = ManagerLogger.CreateLogger<ManagerEventSubscriptions>();

        #endregion

        #region Instance Fields

        public bool FireAllEvents { get; set; } = false;

        private readonly ConcurrentDictionary<string, ManagerInvokable> _handlers;
        private readonly Channel<Tuple<object?, IManagerEvent>> _eventChannel;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _consumerTask;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor for standard usage (created by ManagerConnection).
        /// Will be disposed automatically when the connection is disposed.
        /// </summary>
        public ManagerEventSubscriptions()
        {
            _handlers = new ConcurrentDictionary<string, ManagerInvokable>();
            _eventChannel = Channel.CreateUnbounded<Tuple<object?, IManagerEvent>>(new UnboundedChannelOptions { SingleReader = true });
            _cancellationTokenSource = new CancellationTokenSource();

            _consumerTask = ConsumeEventsAsync(_cancellationTokenSource.Token);

            _logger.LogDebug("ManagerEventSubscriptions created with standard disposal behavior");
        }

        #endregion

        #region Public Subscription API

        /// <summary>
        /// Subscribe to events of a specific type.
        /// </summary>
        /// <typeparam name="T">The type of event to subscribe to</typeparam>
        /// <param name="action">The event handler to invoke when the event occurs</param>
        /// <returns>A disposable object to unsubscribe from the event</returns>
        public IDisposable On<T>(EventHandler<T> action) where T : IManagerEvent
        {
            string eventKey = ManagerEventBuilder.GetEventKey<T>();

            var invokable = _handlers.GetOrAdd(eventKey, key =>
            {
                var newHandler = new ManagerEventSubscription<T>(key);
                newHandler.OnChanged += OnHandlerChanged;
                return newHandler;
            });

            if (invokable is ManagerEventSubscription<T> handler)
                return new DisposableHandler<T>(handler, action);

            throw new InvalidOperationException($"Handler type mismatch for event key: {eventKey}.");
        }

        /// <summary>
        /// Handles cleanup when an event handler has no more subscribers.
        /// </summary>
        /// <param name="sender">The handler that changed</param>
        /// <param name="e">Event arguments</param>
        private void OnHandlerChanged(object? sender, EventArgs e)
        {
            if (sender is ManagerInvokable handler && handler.Count == 0)
                _handlers.TryRemove(handler.Key, out _);
        }

        #endregion

        #region Event Dispatching (Producer-Consumer Model)

        /// <summary>
        /// Dispatches an event to all registered handlers.
        /// Uses high-performance producer-consumer pattern with channels.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The event to dispatch</param>
        public void Dispatch(object? sender, IManagerEvent e)
        {
            // Check if we're disposed before attempting to write to the channel
            if (IsDisposed || _cancellationTokenSource.IsCancellationRequested)
            {
                _logger.LogDebug("Event of type {EventType} was discarded because the subscription system is disposed or being disposed.", e.GetType().Name);
                return;
            }

            try
            {
                if (!_eventChannel.Writer.TryWrite(Tuple.Create(sender, e)))
                {
                    // Only log as warning if we're not being disposed
                    if (!IsDisposed && !_cancellationTokenSource.IsCancellationRequested)
                    {
                        _logger.LogWarning("Event channel is full or closed. Event of type {EventType} was dropped.", e.GetType().Name);
                    }
                    else
                    {
                        _logger.LogDebug("Event of type {EventType} was discarded during disposal process.", e.GetType().Name);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Channel writer is completed, this is expected during disposal
                if (!IsDisposed && !_cancellationTokenSource.IsCancellationRequested)
                {
                    _logger.LogWarning("Attempted to write to a completed channel. Event of type {EventType} was dropped.", e.GetType().Name);
                }
                else
                {
                    _logger.LogDebug("Event of type {EventType} was discarded because channel is completed during disposal.", e.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error dispatching event of type {EventType}.", e.GetType().Name);
            }
        }

        /// <summary>
        /// Background task that consumes events from the channel and dispatches them to handlers.
        /// Runs for the lifetime of the subscription system.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the async operation</returns>
        private async Task ConsumeEventsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Event consumer task started.");
            try
            {
                await foreach (var (sender, evt) in _eventChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try 
                    { 
                        DispatchInternal(sender, evt); 
                    }
                    catch (Exception ex) 
                    { 
                        _logger.LogError(ex, "Error during internal dispatch of event {EventType}.", evt.GetType().Name); 
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Event consumer task was canceled gracefully.");
            }
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "The event consumer loop has terminated due to an unhandled exception."); 
            }
            finally 
            { 
                _logger.LogInformation("Event consumer task finished."); 
            }
        }

        /// <summary>
        /// Internal method that actually dispatches events to registered handlers.
        /// Handles both specific and abstract event type matching.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The event to dispatch</param>
        private void DispatchInternal(object? sender, IManagerEvent e)
        {
            string eventKey = ManagerEventBuilder.GetEventKey(e);
            bool wasHandled = false;

            // Try specific handler first
            if (_handlers.TryGetValue(eventKey, out var handler))
            {
                handler.Invoke(sender, e);
                wasHandled = true;
            }

            // Try abstract/base type handlers
            var eventType = e.GetType();
            var abstractHandlers = _handlers.Values.Where(h => 
                h.GetType().GetGenericArguments()[0].IsAssignableFrom(eventType) && 
                h.Key != eventKey);
                
            foreach (var abstractHandler in abstractHandlers)
            {
                abstractHandler.Invoke(sender, e);
                wasHandled = true;
            }

            // Fire unhandled event if no specific handlers and FireAllEvents is enabled
            if (!wasHandled && FireAllEvents)
            {
                UnhandledEvent?.Invoke(sender, e);
            }
        }

        #endregion

        #region User Event Registration

        /// <summary>
        /// Registers a custom user event class for parsing specific user-defined events.
        /// Delegates to ManagerEventBuilder for actual registration.
        /// </summary>
        /// <param name="userEventClass">The type of the user event to register</param>
        public void RegisterUserEventClass(Type userEventClass)
        {
            ManagerEventBuilder.RegisterUserEventClass(userEventClass);
        }

        #endregion

        #region Disposal (Clean Standard Behavior)

        /// <summary>
        /// Gets a value indicating whether this instance has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Event triggered when an unhandled event is received.
        /// </summary>
        public event EventHandler<IManagerEvent>? UnhandledEvent;

        /// <summary>
        /// Standard synchronous dispose method.
        /// Always disposes the instance (no ownership complexity).
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;
            
            try
            {
                // For sync dispose, we need to wait for async operations to complete
                DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during synchronous dispose");
            }
        }

        /// <summary>
        /// Standard asynchronous dispose method.
        /// Always disposes the instance (no ownership complexity).
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (IsDisposed) return;
            
            _logger.LogInformation("Starting disposal of ManagerEventSubscriptions");
            
            // Mark as disposed early to prevent new events from being dispatched
            IsDisposed = true;

            try
            {
                // 1. Signal cancellation to stop accepting new events
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }

                // 2. Complete the channel writer to stop accepting new events
                // Use a try-catch as the writer might already be completed
                try
                {
                    _eventChannel.Writer.Complete();
                }
                catch (InvalidOperationException)
                {
                    // Writer was already completed, this is fine
                    _logger.LogDebug("Channel writer was already completed during disposal");
                }

                // 3. Wait for the consumer task to finish processing remaining events
                if (_consumerTask != null && !_consumerTask.IsCompleted)
                {
                    try
                    {
                        // Wait for the consumer task to finish processing any remaining events.
                        // It should complete as the channel is marked as complete and the cancellation token is triggered.
                        await _consumerTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancellation token is triggered
                        _logger.LogDebug("Consumer task completed via cancellation.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error waiting for consumer task to complete");
                    }
                }

                // 4. Clear event handlers and unsubscribe from change notifications
                foreach (var handler in _handlers.Values)
                {
                    try
                    {
                        handler.OnChanged -= OnHandlerChanged;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error unsubscribing from handler change notifications");
                    }
                }

                // 5. Clear all collections
                _handlers.Clear();
                UnhandledEvent = null;

                // 6. Dispose cancellation token source
                _cancellationTokenSource?.Dispose();

                _logger.LogInformation("ManagerEventSubscriptions disposal completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ManagerEventSubscriptions disposal");
                throw;
            }
        }

        /// <summary>
        /// Finalizer to ensure resources are cleaned up if Dispose is not called.
        /// </summary>
        ~ManagerEventSubscriptions()
        {
            if (!IsDisposed)
            {
                _logger.LogWarning("ManagerEventSubscriptions was not properly disposed. Consider using 'using' statement or calling Dispose/DisposeAsync explicitly.");
                
                // For finalizer, we can only do synchronous cleanup
                try
                {
                    IsDisposed = true;
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                    _handlers.Clear();
                    UnhandledEvent = null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during finalization");
                }
            }
        }

        #endregion
    }
}