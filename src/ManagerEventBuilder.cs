using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Connection;
using Sufficit.Asterisk.Manager.Events;
using Sufficit.Json;

namespace Sufficit.Asterisk.Manager
{
    /// <summary>
    /// Static factory class responsible for building ManagerEvent instances from AMI packet attributes.
    /// Handles event type discovery, registration, and instantiation for Asterisk Manager Interface events.
    /// </summary>
    /// <remarks>
    /// This class provides:
    /// - Event type discovery from assemblies
    /// - Event class registration and caching
    /// - Event instance creation from packet dictionaries
    /// - Action ID parsing and internal ID handling
    /// - User event support and registration
    /// 
    /// Thread-safe and optimized for high-performance event parsing.
    /// </remarks>
    public static class ManagerEventBuilder
    {
        #region Static Fields and Logger

        private static readonly ILogger _logger = ManagerLogger.CreateLogger(typeof(ManagerEventBuilder));
        private static readonly object _lockDiscovered = new object();
        private static readonly Dictionary<string, ConstructorInfo> _registeredEventClasses;
        private static IEnumerable<Type>? _discoveredTypes;

        #endregion

        #region Static Constructor

        static ManagerEventBuilder()
        {
            _registeredEventClasses = new Dictionary<string, ConstructorInfo>();
            RegisterBuiltinEventClasses();
        }

        #endregion

        #region Event Key Generation

        /// <summary>
        /// Generates an event key for a specific event type.
        /// </summary>
        /// <typeparam name="T">The event type implementing IManagerEvent</typeparam>
        /// <returns>The event key string</returns>
        public static string GetEventKey<T>() where T : IManagerEvent => GetEventKey(typeof(T));

        /// <summary>
        /// Generates an event key for a specific event instance.
        /// </summary>
        /// <param name="e">The event instance</param>
        /// <returns>The event key string</returns>
        public static string GetEventKey(IManagerEvent e) => GetEventKey(e.GetType());

        /// <summary>
        /// Generates an event key for a specific event type.
        /// </summary>
        /// <param name="e">The event type</param>
        /// <returns>The event key string</returns>
        public static string GetEventKey(Type e) => GetEventKey(e.Name);

        /// <summary>
        /// Generates an event key from an event name string.
        /// Normalizes the event name by converting to lowercase and removing "event" suffix.
        /// </summary>
        /// <param name="event">The event name string</param>
        /// <returns>The normalized event key</returns>
        /// <exception cref="ArgumentNullException">Thrown when event name is null or whitespace</exception>
        public static string GetEventKey(string @event)
        {
            if (string.IsNullOrWhiteSpace(@event))
                throw new ArgumentNullException(nameof(@event));

            var key = @event.Trim().ToLowerInvariant();
            if (key.EndsWith("event"))
                key = key.Substring(0, key.Length - 5);
            return key;
        }

        #endregion

        #region Action ID Processing

        /// <summary>
        /// Strips the internal action ID prefix from an action ID string.
        /// </summary>
        /// <param name="actionId">The full action ID including internal prefix</param>
        /// <returns>The action ID without internal prefix</returns>
        private static string StripInternalActionId(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return string.Empty;
            int delimiterIndex = actionId.IndexOf(Common.INTERNAL_ACTION_ID_DELIMITER);
            if (delimiterIndex < 0) return actionId;
            return actionId.Length > delimiterIndex + 1
                ? actionId.Substring(delimiterIndex + 1).Trim()
                : string.Empty;
        }

        /// <summary>
        /// Extracts the internal action ID prefix from an action ID string.
        /// </summary>
        /// <param name="actionId">The full action ID including internal prefix</param>
        /// <returns>The internal action ID prefix</returns>
        private static string GetInternalActionId(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return string.Empty;
            int delimiterIndex = actionId.IndexOf(Common.INTERNAL_ACTION_ID_DELIMITER);
            return delimiterIndex > 0 ? actionId.Substring(0, delimiterIndex).Trim() : string.Empty;
        }

        #endregion

        #region Assembly and Type Discovery

        /// <summary>
        /// Determines if an assembly should be included in event type discovery.
        /// </summary>
        /// <param name="assembly">The assembly to check</param>
        /// <returns>True if the assembly should be searched for event types</returns>
        private static bool AssemblyMatch(Assembly assembly)
        {
            return !assembly.IsDynamic && assembly.FullName != null &&
                   (assembly.FullName.StartsWith(nameof(Sufficit), StringComparison.InvariantCultureIgnoreCase) ||
                    assembly.FullName.StartsWith(nameof(AsterNET), StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Discovers all event types from relevant assemblies.
        /// Thread-safe and cached for performance.
        /// </summary>
        /// <returns>Collection of discovered event types</returns>
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
                            discovered.AddRange(types.Where(type => 
                                type.IsPublic && 
                                !type.IsAbstract && 
                                managerInterface.IsAssignableFrom(type)));
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
                    _logger.LogDebug("Discovered {Count} event types from {AssemblyCount} assemblies", 
                        discovered.Count, assemblies.Count(AssemblyMatch));
                }
                return _discoveredTypes;
            }
        }

        #endregion

        #region Event Class Registration

        /// <summary>
        /// Registers a single event class for event building.
        /// </summary>
        /// <param name="clazz">The event class type to register</param>
        private static void RegisterEventClass(Type clazz)
        {
            if (clazz.IsAbstract || !typeof(IManagerEvent).IsAssignableFrom(clazz)) return;

            string eventKey = GetEventKey(clazz);
            if (typeof(UserEvent).IsAssignableFrom(clazz) && !eventKey.StartsWith("user", StringComparison.OrdinalIgnoreCase))
                eventKey = "user" + eventKey;

            if (_registeredEventClasses.ContainsKey(eventKey)) return;

            var constructor = clazz.GetConstructor(Type.EmptyTypes) ?? clazz.GetConstructor(new[] { typeof(ManagerConnection) });

            if (constructor != null && constructor.IsPublic)
            {
                _registeredEventClasses.Add(eventKey, constructor);
                _logger.LogTrace("Registered event class: {EventKey} -> {TypeName}", eventKey, clazz.FullName);
            }
            else
            {
                _logger.LogWarning("RegisterEventClass: {TypeName} has no public default or (ManagerConnection) constructor and will be ignored.", clazz.FullName);
            }
        }

        /// <summary>
        /// Registers all built-in event classes discovered from assemblies.
        /// </summary>
        private static void RegisterBuiltinEventClasses()
        {
            foreach (var type in GetDiscoveredTypes())
                RegisterEventClass(type);
                
            _logger.LogInformation("Registered {Count} built-in event classes", _registeredEventClasses.Count);
        }

        /// <summary>
        /// Registers a custom user event class for parsing specific user-defined events.
        /// This method is thread-safe and can be called at runtime.
        /// </summary>
        /// <param name="userEventClass">The type of the user event to register</param>
        /// <exception cref="ArgumentNullException">Thrown when userEventClass is null</exception>
        /// <exception cref="ArgumentException">Thrown when userEventClass doesn't implement IManagerEvent</exception>
        public static void RegisterUserEventClass(Type userEventClass)
        {
            if (userEventClass == null) 
                throw new ArgumentNullException(nameof(userEventClass));
            
            if (!typeof(IManagerEvent).IsAssignableFrom(userEventClass))
                throw new ArgumentException("Type must implement IManagerEvent.", nameof(userEventClass));

            lock (_lockDiscovered)
            {
                RegisterEventClass(userEventClass);
            }
        }

        #endregion

        #region Event Building

        /// <summary>
        /// Builds a ManagerEventGeneric instance from AMI packet attributes.
        /// This is the main entry point for converting raw AMI data into typed event objects.
        /// </summary>
        /// <param name="attributes">Dictionary of attribute key-value pairs from AMI packet</param>
        /// <returns>A ManagerEventGeneric instance containing the parsed event, or null if no event could be built</returns>
        /// <remarks>
        /// This method:
        /// 1. Extracts the event name from attributes
        /// 2. Determines the appropriate event class to instantiate
        /// 3. Creates the event instance using reflection
        /// 4. Populates the event with attributes using ManagerResponseBuilder
        /// 5. Handles special processing for response events (ActionId parsing)
        /// 
        /// Performance optimized with cached constructors and minimal allocations.
        /// </remarks>
        public static ManagerEventGeneric? Build(IDictionary<string, string> attributes)
        {
            if (!attributes.TryGetValue("event", out var eventName)) 
            {
                _logger.LogDebug("No 'event' key found in attributes");
                return null;
            }

            string eventKey = GetEventKey(eventName);
            
            // Handle user events with special naming
            if (eventKey == "user" && attributes.TryGetValue("userevent", out var userEventName) && !string.IsNullOrWhiteSpace(userEventName))
            {
                eventKey = "user" + userEventName.Trim().ToLowerInvariant();
            }

            // Try to find registered constructor
            _registeredEventClasses.TryGetValue(eventKey, out var constructor);

            IManagerEvent genericEvent;
            if (constructor != null)
            {
                try 
                { 
                    genericEvent = (IManagerEvent)constructor.Invoke(null);
                    _logger.LogTrace("Created event instance for key: {EventKey}", eventKey);
                }
                catch (Exception ex) 
                { 
                    _logger.LogError(ex, "Unable to create new instance of event key: {EventKey}", eventKey); 
                    return null; 
                }
            }
            else 
            { 
                // Fallback to unknown event
                genericEvent = new UnknownEvent();
                
                // Smart logging: Only log once per unknown event type to avoid spam
                // For user events, be more specific about what's missing
                if (eventKey.StartsWith("user"))
                {
                    LogUnknownUserEvent(eventKey, attributes);
                }
                else
                {
                    LogUnknownEvent(eventKey, attributes);
                }
            }

            // Create the generic wrapper
            var managerEvent = new ManagerEventGeneric(genericEvent);
            
            // Populate attributes using the response builder
            try
            {
                ManagerResponseBuilder.SetAttributes(managerEvent, attributes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting attributes for event key: {EventKey}", eventKey);
                return null;
            }

            // Handle response events with action IDs
            if (managerEvent.Event is IResponseEvent responseEvent && responseEvent.ActionId != null)
            {
                responseEvent.InternalActionId = GetInternalActionId(responseEvent.ActionId);
                responseEvent.ActionId = StripInternalActionId(responseEvent.ActionId);
            }

            return managerEvent;
        }

        #region Smart Logging for Unknown Events

        private static readonly HashSet<string> _loggedUnknownEvents = new HashSet<string>();
        private static readonly object _loggedEventsLock = new object();

        /// <summary>
        /// Logs unknown user events with helpful information for registration.
        /// Only logs each unique user event type once to prevent log spam.
        /// </summary>
        /// <param name="eventKey">The unknown user event key</param>
        /// <param name="attributes">The event attributes for context</param>
        private static void LogUnknownUserEvent(string eventKey, IDictionary<string, string> attributes)
        {
            lock (_loggedEventsLock)
            {
                if (_loggedUnknownEvents.Add(eventKey))
                {
                    // First time seeing this user event - log with full details
                    var userEventName = attributes.TryGetValue("userevent", out var name) ? name : "Unknown";
                    var jsonAttributes = attributes.ToJson();
                    
                    _logger.LogInformation("Unknown UserEvent '{UserEventName}' (key: {EventKey}) - consider registering a custom class. Attributes: {Attributes}", 
                        userEventName, eventKey, jsonAttributes);
                        
                    // Provide helpful registration hint
                    _logger.LogInformation("To register this UserEvent, create a class inheriting from UserEvent and call: ManagerEventBuilder.RegisterUserEventClass(typeof(YourCustomEvent))");
                }
                else
                {
                    // Subsequent occurrences - minimal logging
                    _logger.LogTrace("Using UnknownEvent for unregistered user event: {EventKey}", eventKey);
                }
            }
        }

        /// <summary>
        /// Logs unknown standard events with context information.
        /// Only logs each unique event type once to prevent log spam.
        /// </summary>
        /// <param name="eventKey">The unknown event key</param>
        /// <param name="attributes">The event attributes for context</param>
        private static void LogUnknownEvent(string eventKey, IDictionary<string, string> attributes)
        {
            lock (_loggedEventsLock)
            {
                if (_loggedUnknownEvents.Add(eventKey))
                {
                    // First time seeing this event - log with moderate detail
                    var originalEventName = attributes.TryGetValue("event", out var name) ? name : "Unknown";
                    
                    _logger.LogWarning("Unknown AMI event '{OriginalEventName}' (key: {EventKey}) - using UnknownEvent fallback. Consider implementing this event type.", 
                        originalEventName, eventKey);
                        
                    // Log attributes only at debug level to avoid spam
                    _logger.LogDebug("Unknown event attributes: {Attributes}", attributes.ToJson());
                }
                else
                {
                    // Subsequent occurrences - trace level only
                    _logger.LogTrace("Using UnknownEvent for unregistered event: {EventKey}", eventKey);
                }
            }
        }

        /// <summary>
        /// Gets information about unknown events that have been logged.
        /// Useful for diagnostics and identifying events that need custom implementations.
        /// </summary>
        /// <returns>Collection of unknown event keys that have been encountered</returns>
        public static IReadOnlyCollection<string> GetUnknownEvents()
        {
            lock (_loggedEventsLock)
            {
                return _loggedUnknownEvents.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Clears the unknown events log cache.
        /// Useful for testing or when you want to see the "first occurrence" logs again.
        /// </summary>
        public static void ClearUnknownEventsLog()
        {
            lock (_loggedEventsLock)
            {
                _loggedUnknownEvents.Clear();
            }
        }

        #endregion

        #endregion

        #region Statistics and Diagnostics

        /// <summary>
        /// Gets the count of currently registered event classes.
        /// </summary>
        /// <returns>Number of registered event classes</returns>
        public static int RegisteredEventClassCount => _registeredEventClasses.Count;

        /// <summary>
        /// Gets a read-only copy of all registered event class keys.
        /// </summary>
        /// <returns>Collection of registered event keys</returns>
        public static IReadOnlyCollection<string> RegisteredEventKeys => _registeredEventClasses.Keys.ToList().AsReadOnly();

        /// <summary>
        /// Checks if a specific event key is registered.
        /// </summary>
        /// <param name="eventKey">The event key to check</param>
        /// <returns>True if the event key is registered</returns>
        public static bool IsEventKeyRegistered(string eventKey) => _registeredEventClasses.ContainsKey(eventKey);

        #endregion
    }
}