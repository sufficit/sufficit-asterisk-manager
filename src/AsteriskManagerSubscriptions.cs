using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Connection;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager
{
    /// <summary>
    /// Central event subscription management system for Asterisk Manager Interface (AMI) events.
    /// Coordinates multiple event subscriptions and handles event dispatching using a modern,
    /// high-performance, non-blocking producer-consumer pattern specifically for AMI events.
    /// </summary>
    /// <remarks>Designed to be used on multiple instances of <see cref="ManagerConnection"/></remarks>
    public class ManagerEventSubscriptions : IManagerEventSubscriptions, IAsyncDisposable
    {
        #region Static Section (Original Logic)

        private static readonly ILogger _logger = ManagerLogger.CreateLogger<ManagerEventSubscriptions>();
        private static readonly object _lockDiscovered = new object();
        private static IEnumerable<Type>? _discoveredTypes;

        public static string GetEventKey<T>() where T : IManagerEvent => GetEventKey(typeof(T));
        public static string GetEventKey(IManagerEvent e) => GetEventKey(e.GetType());
        public static string GetEventKey(Type e) => GetEventKey(e.Name);
        public static string GetEventKey(string @event)
        {
            if (string.IsNullOrWhiteSpace(@event))
                throw new ArgumentNullException(nameof(@event));

            var key = @event.Trim().ToLowerInvariant();
            if (key.EndsWith("event"))
                key = key.Substring(0, key.Length - 5);
            return key;
        }

        private static string StripInternalActionId(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return string.Empty;
            int delimiterIndex = actionId.IndexOf(Common.INTERNAL_ACTION_ID_DELIMITER);
            if (delimiterIndex < 0) return actionId;
            return actionId.Length > delimiterIndex + 1
                ? actionId.Substring(delimiterIndex + 1).Trim()
                : string.Empty;
        }

        private static string GetInternalActionId(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return string.Empty;
            int delimiterIndex = actionId.IndexOf(Common.INTERNAL_ACTION_ID_DELIMITER);
            return delimiterIndex > 0 ? actionId.Substring(0, delimiterIndex).Trim() : string.Empty;
        }

        private static bool AssemblyMatch(Assembly assembly)
        {
            return !assembly.IsDynamic && assembly.FullName != null &&
                   (assembly.FullName.StartsWith(nameof(Sufficit), StringComparison.InvariantCultureIgnoreCase) ||
                    assembly.FullName.StartsWith(nameof(AsterNET), StringComparison.InvariantCultureIgnoreCase));
        }

        private static IEnumerable<Type> GetDiscoveredTypes()
        {
            lock (_lockDiscovered)
            {
                if (_discoveredTypes == null)
                {
                    var managerInterface = typeof(IManagerEvent);
                    var discovered = new List<Type>();
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (var assembly in assemblies.Where(AssemblyMatch))
                    {
                        try
                        {
                            var types = assembly.GetTypes();
                            discovered.AddRange(types.Where(type => type.IsPublic && !type.IsAbstract && managerInterface.IsAssignableFrom(type)));
                        }
                        catch (ReflectionTypeLoadException typeLoadException)
                        {
                            foreach (var loaderException in typeLoadException.LoaderExceptions.Where(ex => ex != null))
                                _logger.LogError(loaderException, "Error getting types on assembly: {assembly}", assembly.FullName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Generic error getting types on assembly: {assembly}", assembly.FullName);
                        }
                    }
                    _discoveredTypes = discovered;
                }
                return _discoveredTypes;
            }
        }

        private static void RegisterEventClass(Dictionary<string, ConstructorInfo> list, Type clazz)
        {
            if (clazz.IsAbstract || !typeof(IManagerEvent).IsAssignableFrom(clazz)) return;

            string eventKey = GetEventKey(clazz);
            if (typeof(UserEvent).IsAssignableFrom(clazz) && !eventKey.StartsWith("user", StringComparison.OrdinalIgnoreCase))
                eventKey = "user" + eventKey;

            if (list.ContainsKey(eventKey)) return;

            var constructor = clazz.GetConstructor(Type.EmptyTypes) ?? clazz.GetConstructor(new[] { typeof(ManagerConnection) });

            if (constructor != null && constructor.IsPublic)
                list.Add(eventKey, constructor);
            else
                _logger.LogWarning("RegisterEventClass: {TypeName} has no public default or (ManagerConnection) constructor and will be ignored.", clazz.FullName);
        }

        private static void RegisterBuiltinEventClasses(Dictionary<string, ConstructorInfo> list)
        {
            foreach (var type in GetDiscoveredTypes())
                RegisterEventClass(list, type);
        }
        #endregion

        #region Instance Section (Refactored Logic)

        public bool FireAllEvents { get; set; } = false;

        private readonly ConcurrentDictionary<string, ManagerInvokable> _handlers;
        private readonly Channel<Tuple<object?, IManagerEvent>> _eventChannel;
        private readonly Dictionary<string, ConstructorInfo> _registeredEventClasses;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _consumerTask;

        public ManagerEventSubscriptions()
        {
            _handlers = new ConcurrentDictionary<string, ManagerInvokable>();
            _eventChannel = Channel.CreateUnbounded<Tuple<object?, IManagerEvent>>(new UnboundedChannelOptions { SingleReader = true });
            _cancellationTokenSource = new CancellationTokenSource();

            _registeredEventClasses = new Dictionary<string, ConstructorInfo>();
            RegisterBuiltinEventClasses(_registeredEventClasses);

            _consumerTask = ConsumeEventsAsync(_cancellationTokenSource.Token);
        }

        #region Public Subscription API

        public IDisposable On<T>(EventHandler<T> action) where T : IManagerEvent
        {
            string eventKey = GetEventKey<T>();

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

        private void OnHandlerChanged(object? sender, EventArgs e)
        {
            if (sender is ManagerInvokable handler && handler.Count == 0)
                _handlers.TryRemove(handler.Key, out _);
        }

        #endregion

        #region Event Dispatching (Producer-Consumer Model)

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

        private async Task ConsumeEventsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Event consumer task started.");
            try
            {
                await foreach (var (sender, evt) in _eventChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try { DispatchInternal(sender, evt); }
                    catch (Exception ex) { _logger.LogError(ex, "Error during internal dispatch of event {EventType}.", evt.GetType().Name); }
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

        private void DispatchInternal(object? sender, IManagerEvent e)
        {
            string eventKey = GetEventKey(e);
            bool wasHandled = false;

            if (_handlers.TryGetValue(eventKey, out var handler))
            {
                handler.Invoke(sender, e);
                wasHandled = true;
            }

            var eventType = e.GetType();
            var abstractHandlers = _handlers.Values.Where(h => h.GetType().GetGenericArguments()[0].IsAssignableFrom(eventType) && h.Key != eventKey);
            foreach (var abstractHandler in abstractHandlers)
            {
                abstractHandler.Invoke(sender, e);
                wasHandled = true;
            }

            if (!wasHandled && FireAllEvents)
            {
                UnhandledEvent?.Invoke(sender, e);
            }
        }

        #endregion

        #region Build and Dispose

        public ManagerEventGeneric? Build(IDictionary<string, string> attributes)
        {
            if (!attributes.TryGetValue("event", out var eventName)) return null;

            string eventKey = GetEventKey(eventName);
            if (eventKey == "user" && attributes.TryGetValue("userevent", out var userEventName) && !string.IsNullOrWhiteSpace(userEventName))
            {
                eventKey = "user" + userEventName.Trim().ToLowerInvariant();
            }

            _registeredEventClasses.TryGetValue(eventKey, out var constructor);

            IManagerEvent genericEvent;
            if (constructor != null)
            {
                try { genericEvent = (IManagerEvent)constructor.Invoke(null); }
                catch (Exception ex) { _logger.LogError(ex, "Unable to create new instance of {eventKey}", eventKey); return null; }
            }
            else { genericEvent = new UnknownEvent(); }

            var e = new ManagerEventGeneric(genericEvent);
            ManagerResponseBuilder.SetAttributes(e, attributes);

            if (e.Event is IResponseEvent responseEvent && responseEvent.ActionId != null)
            {
                responseEvent.InternalActionId = GetInternalActionId(responseEvent.ActionId);
                responseEvent.ActionId = StripInternalActionId(responseEvent.ActionId);
            }
            return e;
        }

        public void RegisterUserEventClass(Type userEventClass)
            => RegisterEventClass(_registeredEventClasses, userEventClass);

        #region DISPOSE

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Synchronous dispose method for IDisposable compatibility
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
        /// Proper async dispose implementation
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
                        // Give a reasonable timeout for the consumer to finish
                        using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                        {
                            var completedTask = await Task.WhenAny(_consumerTask, Task.Delay(Timeout.Infinite, timeoutCts.Token)).ConfigureAwait(false);
                            if (completedTask == _consumerTask)
                            {
                                // Consumer task completed normally, await it to get any exceptions
                                await _consumerTask.ConfigureAwait(false);
                            }
                            else
                            {
                                _logger.LogWarning("Consumer task did not complete within timeout during disposal");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancellation token is triggered or timeout occurs
                        _logger.LogDebug("Consumer task completed via cancellation or timeout");
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
                _registeredEventClasses.Clear();
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
        /// Finalizer to ensure resources are cleaned up if Dispose is not called
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
                    _registeredEventClasses.Clear();
                    UnhandledEvent = null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during finalization");
                }
            }
        }

        #endregion

        public event EventHandler<IManagerEvent>? UnhandledEvent;

        #endregion

        #endregion
    }
}