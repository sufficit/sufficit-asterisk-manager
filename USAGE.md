# Usage Examples - Sufficit.Asterisk.Manager

## Two Complementary Approaches

This library provides two distinct patterns for different use cases:

### ? AsteriskManagerProvider - Quick Operations
**Purpose**: Temporary connections for specific operations

**Use when you need:**
- ? **Quick scripts** or automation tasks
- ? **One-time operations** (originate call, check status, reload config)
- ? **Scheduled tasks** that run periodically
- ? **Full control** over connection lifecycle
- ? **Minimal resource usage** for simple tasks
- ? **Connect, execute, disconnect** pattern

```csharp
// Quick operation example
using var provider = new AsteriskManagerProvider(options, logger);
using var connection = await provider.ConnectAsync(keepAlive: false);
var response = await connection.SendActionAsync(myAction);
// Automatically disconnects when disposed
```

### ??? AsteriskManagerService - Multi-Provider Services  
**Purpose**: Foundation for persistent, multi-server applications

**Use when you need:**
- ? **Multiple Asterisk servers** management
- ? **Automatic reconnection** handling
- ? **Event monitoring** infrastructure
- ? **Background services** foundation
- ? **Reusable service architecture**

```csharp
// Multi-provider service example
public class MyAsteriskService : AsteriskManagerService
{
    public override IManagerEventSubscriptions Events { get; }
    
    protected override IManagerEventSubscriptions GetEventHandler()
    {
        var events = new ManagerEventSubscriptions();
        events.On<NewChannelEvent>(OnNewChannel);
        return events;
    }
    
    protected override IEnumerable<AMIProviderOptions> GetProviderConfigurations()
    {
        return myProviderConfigurations;
    }
    
    // Add your own business-specific commands
    public async Task GetSystemStatus()
    {
        var provider = Providers.FirstOrDefault();
        if (provider?.Connection != null)
        {
            await provider.Connection.SendActionAsync(new CoreShowChannelsAction());
        }
    }
}
```

## Quick Start

### ? Quick Operations

```csharp
// Configure provider
var options = Options.Create(new AMIProviderOptions 
{
    Address = "asterisk.example.com",
    Username = "manager",
    Password = "secret"
});

// Execute quick operation
using var provider = new AsteriskManagerProvider(options, logger);
using var connection = await provider.ConnectAsync(keepAlive: false);

var originateAction = new OriginateAction
{
    Channel = "SIP/1000",
    Context = "default",
    Exten = "1001"
};

var response = await connection.SendActionAsync(originateAction);
```

### ??? Multi-Provider Service

```csharp
// Inherit from base class
public class MyTelephonyService : AsteriskManagerService
{
    public override IManagerEventSubscriptions Events { get; }
    
    public MyTelephonyService(ILoggerFactory loggerFactory) 
        : base(loggerFactory)
    {
        Events = GetEventHandler();
    }
    
    protected override IManagerEventSubscriptions GetEventHandler()
    {
        var events = new ManagerEventSubscriptions();
        events.FireAllEvents = true;
        
        // Subscribe to events
        events.On<NewChannelEvent>(OnNewChannel);
        events.On<HangupEvent>(OnHangup);
        events.On<PeerStatusEvent>(OnPeerStatus);
        
        return events;
    }
    
    protected override IEnumerable<AMIProviderOptions> GetProviderConfigurations()
    {
        return new[]
        {
            new AMIProviderOptions
            {
                Title = "Primary Server",
                Address = "asterisk1.example.com",
                Username = "manager",
                Password = "secret",
                KeepAlive = true
            },
            new AMIProviderOptions
            {
                Title = "Backup Server", 
                Address = "asterisk2.example.com",
                Username = "manager",
                Password = "secret",
                KeepAlive = true
            }
        };
    }
    
    private void OnNewChannel(object? sender, NewChannelEvent evt)
    {
        Console.WriteLine($"New call: {evt.CallerIdNumber} -> {evt.Exten}");
    }
    
    private void OnHangup(object? sender, HangupEvent evt)
    {
        Console.WriteLine($"Call ended: {evt.Channel}");
    }
    
    private void OnPeerStatus(object? sender, PeerStatusEvent evt)
    {
        Console.WriteLine($"Peer {evt.Peer}: {evt.PeerStatus}");
    }
}

// Usage
var service = new MyTelephonyService(loggerFactory);
await service.StartAsync();

// Service now monitors multiple Asterisk servers
// with automatic reconnection and event handling
```

## Configuration

### Basic Provider Configuration

```json
{
  "AMIProviderOptions": {
    "Address": "asterisk.example.com",
    "Port": 5038,
    "Username": "manager", 
    "Password": "secret",
    "KeepAlive": false,
    "UseMD5Authentication": true,
    "SocketEncoding": "ASCII"
  }
}
```

### Multi-Provider Configuration

```json
{
  "Providers": [
    {
      "Enabled": true,
      "Title": "Primary Server",
      "Address": "asterisk1.example.com",
      "Username": "manager",
      "Password": "secret",
      "KeepAlive": true,
      "ReconnectRetryMax": 0,
      "InitialRetry": {
        "EnableInitialRetry": true,
        "MaxInitialRetryAttempts": 0,
        "InitialRetryDelayMs": 5000,
        "MaxRetryDelayMs": 30000
      }
    }
  ]
}
```

## Smart Event Management

### Custom UserEvent Registration

```csharp
// Define custom UserEvent
public class DoQueueStatusUserEvent : UserEvent
{
    public string? QueueName { get; set; }
    public string? Status { get; set; }
    public string? Agent { get; set; }
}

// Register at runtime
ManagerEventBuilder.RegisterUserEventClass(typeof(DoQueueStatusUserEvent));

// Now your custom events will be automatically detected and instantiated
```

### Event Diagnostics and Monitoring

```csharp
// Check if an event type is registered
bool isRegistered = ManagerEventBuilder.IsEventKeyRegistered("userdoqueuestatus");

// Get count of registered event types
int registeredCount = ManagerEventBuilder.RegisteredEventClassCount;

// Get all registered event keys
var allRegistered = ManagerEventBuilder.RegisteredEventKeys;

// Get unknown events that have been encountered
var unknownEvents = ManagerEventBuilder.GetUnknownEvents();
foreach (var eventKey in unknownEvents)
{
    Console.WriteLine($"Unknown event encountered: {eventKey}");
}

// Clear unknown events log (useful for testing)
ManagerEventBuilder.ClearUnknownEventsLog();
```

## Common Use Cases

### Scripts and Automation

```csharp
// Perfect for scheduled tasks, admin scripts
using var provider = new AsteriskManagerProvider(options, logger);
using var connection = await provider.ConnectAsync(keepAlive: false);

// Execute administrative commands
await connection.SendActionAsync(new ReloadAction());
await connection.SendActionAsync(new CoreShowChannelsAction());
```

### Call Center Applications

```csharp
// Perfect for real-time monitoring
public class CallCenterService : AsteriskManagerService
{
    protected override IManagerEventSubscriptions GetEventHandler()
    {
        var events = new ManagerEventSubscriptions();
        events.On<QueueMemberStatusEvent>(UpdateAgentStatus);
        events.On<QueueCallerJoinEvent>(OnCustomerJoin);
        return events;
    }
}
```

### PBX Management Tools

```csharp
// Perfect for configuration management
public async Task ConfigureExtension(string extension, string name)
{
    using var provider = new AsteriskManagerProvider(options, logger);
    using var connection = await provider.ConnectAsync(keepAlive: false);
    
    await connection.SendActionAsync(new DBPutAction 
    { 
        Family = "AMPUSER", 
        Key = $"{extension}/name", 
        Val = name 
    });
    
    await connection.SendActionAsync(new ReloadAction { Module = "app_queue.so" });
}
```

## Performance Comparison

| Approach | Connection Overhead | Memory Usage | Event Support | Smart Logging | Best For |
|----------|-------------------|--------------|---------------|---------------|----------|
| **AsteriskManagerProvider** | Per operation | Low | ? No | ? Yes | Scripts, one-time ops |
| **AsteriskManagerService** | None (persistent) | Medium | ? Real-time | ? Yes | Multi-server services |