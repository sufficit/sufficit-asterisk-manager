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

        private readonly ConcurrentDictionary<string, ManagerInvokable> _handlers;
        private readonly Channel<Tuple<object?, IManagerEvent>> _eventChannel;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _consumerTask;

        // Cache to avoid reflection on every dispatch. Maps event type to a list of handlers.
        private readonly ConcurrentDictionary<Type, ICollection<ManagerInvokable>> _dispatchCache;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor for standard usage (created by ManagerConnection).
        /// Will be disposed automatically when the connection is disposed.
        /// </summary>
        public ManagerEventSubscriptions()
        {
            _handlers = new ConcurrentDictionary<string, ManagerInvokable>();
            _dispatchCache = new ConcurrentDictionary<Type, ICollection<ManagerInvokable>>();
            _eventChannel = Channel.CreateUnbounded<Tuple<object?, IManagerEvent>>(new UnboundedChannelOptions { SingleReader = true });
            _cancellationTokenSource = new CancellationTokenSource();

            // Start the consumer on a dedicated thread to prevent thread pool starvation
            // from interfering with the consumer loop itself. This is crucial if handlers
            // also queue work on the thread pool.
            _consumerTask = Task.Factory.StartNew(
                () => ConsumeEventsAsync(_cancellationTokenSource.Token),
                _cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            ).Unwrap();

            _logger.LogDebug("ManagerEventSubscriptions created with a long-running consumer task.");
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
                _dispatchCache.Clear(); // Invalidate cache on new handler registration
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
            {
                if (_handlers.TryRemove(handler.Key, out _))
                {
                    // Clear the dispatch cache so it can be rebuilt on the next event.
                    _dispatchCache.Clear();
                }
            }
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
                // ReadAllAsync will throw OperationCanceledException when the token is cancelled.
                await foreach (var (sender, evt) in _eventChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    try 
                    { 
                        DispatchInternal(sender, evt); 
                    }
                    catch (Exception ex) 
                    { 
                        _logger.LogError(ex, "Error queueing internal dispatch of event {EventType}.", evt.GetType().Name); 
                    }
                }
            }
            catch (OperationCanceledException)
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
        /// The entire dispatch logic for an event is queued on the thread pool to prevent
        /// the consumer loop from blocking.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The event to dispatch</param>
        private void DispatchInternal(object? sender, IManagerEvent e)
        {
            // Queue the entire dispatch logic to the thread pool.
            _ = Task.Run(() =>
            {
                // Re-check for disposal, as this runs asynchronously.
                if (IsDisposed) return;

                var eventType = e.GetType();
                bool wasHandled = false;

                // Get the list of handlers for this event type from the cache.
                // If not in the cache, build it.
                var applicableHandlers = _dispatchCache.GetOrAdd(eventType, (type) =>
                {
                    // A handler is applicable if the event type can be assigned to the handler's generic type.
                    // This covers specific, base, and interface handlers in a single, clean check.
                    return _handlers.Values.Where(h =>
                        h.GetType().GetGenericArguments()[0].IsAssignableFrom(type)
                    ).ToList();
                });

                if (applicableHandlers.Count > 0)
                {
                    wasHandled = true;
                    foreach (var handler in applicableHandlers)
                    {
                        try
                        {
                            handler.Invoke(sender, e);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in event handler for {EventType} (Handler Key: {HandlerKey})", eventType.Name, handler.Key);
                        }
                    }
                }

                // Fire unhandled event if no specific handlers and UnhandledEvent is set
                if (!wasHandled && UnhandledEvent != null)
                {
                    try
                    {
                        UnhandledEvent.Invoke(sender, e);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in unhandled event handler for {EventType}", e.GetType().Name);
                    }
                }
            });
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
            
            _logger.LogInformation("Starting disposal of ManagerEventSubscriptions.");
            IsDisposed = true;

            try
            {
                // 1. Signal cancellation to stop the consumer task immediately.
                // This is the most direct way to stop the `await foreach` loop.
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }

                // 2. Complete the channel writer. This is a secondary measure to ensure
                // the loop terminates even if cancellation is slow.
                _eventChannel.Writer.TryComplete();

                // 3. Wait for the consumer task to finish with a timeout.
                if (_consumerTask != null && !_consumerTask.IsCompleted)
                {
                    var completedTask = await Task.WhenAny(_consumerTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
                    if (completedTask != _consumerTask)
                    {
                        _logger.LogWarning("Consumer task did not terminate within 2 seconds of cancellation. It may be stuck.");
                    }
                    else
                    {
                        // Await the task to propagate any exceptions (like the expected OperationCanceledException).
                        await _consumerTask.ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // This is the expected outcome of a successful cancellation.
                _logger.LogDebug("Consumer task was cancelled as expected during disposal.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected exception occurred while shutting down the consumer task. Task status: {Status}", _consumerTask?.Status);
            }
            finally
            {
                // Cleanup should happen regardless of whether the task completed gracefully.
                _logger.LogDebug("Proceeding with final resource cleanup.");

                // Clear event handlers and unsubscribe from change notifications
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

                // Clear all collections
                _handlers.Clear();
                UnhandledEvent = null;
                _dispatchCache.Clear();

                // Dispose cancellation token source
                _cancellationTokenSource?.Dispose();

                _logger.LogInformation("ManagerEventSubscriptions disposal completed successfully");
            }
        }

        /// <summary>
        /// Finalizer to ensure resources are cleaned up if Dispose is not called.
        /// </summary>
        ~ManagerEventSubscriptions()
        {
            if (!IsDisposed)
            {
                _logger.LogWarning("ManagerEventSubscriptions was not properly disposed. This can lead to resource leaks. Ensure DisposeAsync() is called.");
                // In a finalizer, we should not call other managed objects' methods (like Dispose).
                // The garbage collector will handle them. We just log a warning.
            }
        }

        #endregion
    }
}