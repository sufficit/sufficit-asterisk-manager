# ManagerEventBuilder Smart Logging

## Overview

The `ManagerEventBuilder` now includes intelligent logging for unknown events to reduce log spam while providing useful diagnostic information.

## Problem Solved

Previously, every unknown event would generate a debug log, causing:
- Log spam for repetitive unknown events
- Difficulty in identifying new unknown event types
- Limited actionable information for developers

## Smart Logging Features

### ?? **Intelligent Event Classification**

1. **UserEvents**: Special handling with registration hints
2. **Standard Events**: Warning level with implementation suggestions
3. **Deduplication**: Only logs each unknown event type once

### ?? **Log Levels by Event Type**

| Event Type | First Occurrence | Subsequent Occurrences |
|------------|------------------|------------------------|
| Unknown UserEvent | `Information` with full details + registration hint | `Trace` |
| Unknown Standard Event | `Warning` with implementation suggestion | `Trace` |

### ?? **Example Logs**

#### UserEvent (First Time):
```
info: ManagerEventBuilder[0]
      Unknown UserEvent 'DoQueueStatus' (key: userdoqueuestatus) - consider registering a custom class. Attributes: {"event":"UserEvent","userevent":"DoQueueStatus",...}

info: ManagerEventBuilder[0]
      To register this UserEvent, create a class inheriting from UserEvent and call: ManagerEventBuilder.RegisterUserEventClass(typeof(YourCustomEvent))
```

#### UserEvent (Subsequent):
```
trce: ManagerEventBuilder[0]
      Using UnknownEvent for unregistered user event: userdoqueuestatus
```

#### Standard Event (First Time):
```
warn: ManagerEventBuilder[0]
      Unknown AMI event 'SomeNewEvent' (key: somenew) - using UnknownEvent fallback. Consider implementing this event type.

dbug: ManagerEventBuilder[0]
      Unknown event attributes: {"event":"SomeNewEvent","property1":"value1",...}
```

## Usage Examples

### Register Custom UserEvent
```csharp
// Create custom event class
public class DoQueueStatusUserEvent : UserEvent
{
    public string? QueueName { get; set; }
    public string? Status { get; set; }
    // ... other properties
}

// Register the class
ManagerEventBuilder.RegisterUserEventClass(typeof(DoQueueStatusUserEvent));
```

### Diagnostic Methods
```csharp
// Get list of unknown events encountered
var unknownEvents = ManagerEventBuilder.GetUnknownEvents();
foreach (var eventKey in unknownEvents)
{
    Console.WriteLine($"Unknown event: {eventKey}");
}

// Clear the log cache (for testing)
ManagerEventBuilder.ClearUnknownEventsLog();

// Check registration status
bool isRegistered = ManagerEventBuilder.IsEventKeyRegistered("userdoqueuestatus");
```

## Benefits

### ? **Reduced Log Noise**
- No more repetitive debug logs for the same unknown events
- Only first occurrence gets detailed logging

### ? **Better Developer Experience**
- Clear registration instructions for UserEvents
- Helpful warnings for missing standard events
- Full attribute details for debugging

### ? **Diagnostic Capabilities**
- Track which unknown events are being encountered
- Get registration status for any event key
- Clear logging cache for testing scenarios

### ? **Performance Optimized**
- Thread-safe deduplication using HashSet
- Minimal overhead for subsequent unknown events
- Efficient lookup for known vs unknown events

## Migration Impact

### Before:
```
dbug: ManagerEventSubscriptions[0]
      Using UnknownEvent for unregistered event key: userdoqueuestatus

dbug: ManagerEventSubscriptions[0]
      Using UnknownEvent for unregistered event key: userdoqueuestatus
      ... (repeated many times)
```

### After:
```
info: ManagerEventBuilder[0]
      Unknown UserEvent 'DoQueueStatus' (key: userdoqueuestatus) - consider registering a custom class. Attributes: {...}

info: ManagerEventBuilder[0]
      To register this UserEvent, create a class inheriting from UserEvent and call: ManagerEventBuilder.RegisterUserEventClass(typeof(YourCustomEvent))

trce: ManagerEventBuilder[0]
      Using UnknownEvent for unregistered user event: userdoqueuestatus
      ... (subsequent occurrences at trace level)
```

## Configuration

To see all unknown event logs, configure your logging level:

```json
{
  "Logging": {
    "LogLevel": {
      "Sufficit.Asterisk.Manager.ManagerEventBuilder": "Trace"
    }
  }
}
```

The smart logging system provides the perfect balance between diagnostic information and log cleanliness, making it easier to identify and implement missing event types while reducing noise in production logs.