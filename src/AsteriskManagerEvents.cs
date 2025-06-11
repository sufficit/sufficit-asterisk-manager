using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using AsterNET;
using AsterNET.Manager.Event;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Connection;
using Sufficit.Asterisk.Manager.Events;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using Sufficit.Json;

namespace Sufficit.Asterisk.Manager
{
    /// <summary>
    /// Manages Asterisk Manager event subscriptions and dispatching using a modern,
    /// high-performance, non-blocking producer-consumer pattern.
    /// </summary>
    /// <remarks>Project to be used on multiple instances of <see cref="ManagerConnection"/></remarks>
    public class AsteriskManagerEvents : IDisposable
    {
        #region Static Section (Original Logic)

        private static ILogger _logger = new LoggerFactory().CreateLogger<AsteriskManagerEvents>();
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

        public static void Log(ILogger logger) => _logger = logger;

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

        public AsteriskManagerEvents()
        {
            _handlers = new ConcurrentDictionary<string, ManagerInvokable>();
            _eventChannel = Channel.CreateUnbounded<Tuple<object?, IManagerEvent>>(new UnboundedChannelOptions { SingleReader = true });

            _registeredEventClasses = new Dictionary<string, ConstructorInfo>();
            RegisterBuiltinEventClasses(_registeredEventClasses);

            _ = ConsumeEventsAsync();
        }

        #region Public Subscription API

        public IDisposable On<T>(EventHandler<T> action) where T : IManagerEvent
        {
            string eventKey = GetEventKey<T>();

            var invokable = _handlers.GetOrAdd(eventKey, key =>
            {
                var newHandler = new ManagerEventHandler<T>(key);
                newHandler.OnChanged += OnHandlerChanged;
                return newHandler;
            });

            if (invokable is ManagerEventHandler<T> handler)
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
            if (!_eventChannel.Writer.TryWrite(Tuple.Create(sender, e)))
                _logger.LogWarning("Event channel is full or closed. Event of type {EventType} was dropped.", e.GetType().Name);
        }

        private async Task ConsumeEventsAsync()
        {
            _logger.LogInformation("Event consumer task started.");
            try
            {
                await foreach (var (sender, evt) in _eventChannel.Reader.ReadAllAsync())
                {
                    try { DispatchInternal(sender, evt); }
                    catch (Exception ex) { _logger.LogError(ex, "Error during internal dispatch of event {EventType}.", evt.GetType().Name); }
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "The event consumer loop has terminated due to an unhandled exception."); }
            finally { _logger.LogInformation("Event consumer task finished."); }
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

        internal ManagerEventGeneric? Build(IDictionary<string, string> attributes)
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

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;

            _eventChannel.Writer.TryComplete();
            UnhandledEvent = null;
            _handlers.Clear();
            _registeredEventClasses.Clear();
        }

        #endregion

        public event EventHandler<IManagerEvent>? UnhandledEvent;

        #endregion

        #endregion
    }
}