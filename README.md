<h1>
  Sufficit.Asterisk.Manager
  <a href="https://github.com/sufficit"><img src="https://avatars.githubusercontent.com/u/66928451?s=200&v=4" alt="Sufficit Logo" width="80" align="right"></a>
</h1>

[![NuGet](https://img.shields.io/nuget/v/Sufficit.Asterisk.Manager.svg)](https://www.nuget.org/packages/Sufficit.Asterisk.Manager/)

## 📖 About the Project

`Sufficit.Asterisk.Manager` provides comprehensive **Asterisk Manager Interface (AMI)** integration for .NET applications, supporting both **quick operations** and **persistent multi-provider services**.

## 🎯 Two Complementary Approaches

This library provides two distinct patterns for different use cases:

### ⚡ AsteriskManagerProvider - Quick Operations
**Purpose**: Temporary connections for specific operations

**Use when you need:**
- ✅ **Quick scripts** or automation tasks
- ✅ **One-time operations** (originate call, check status, reload config)
- ✅ **Scheduled tasks** that run periodically
- ✅ **Full control** over connection lifecycle
- ✅ **Minimal resource usage** for simple tasks
- ✅ **Connect, execute, disconnect** pattern
// Quick operation example
using var provider = new AsteriskManagerProvider(options, logger);
using var connection = await provider.ConnectAsync(keepAlive: false);
var response = await connection.SendActionAsync(myAction);
// Automatically disconnects when disposed
### 🏗️ AsteriskManagerService - Multi-Provider Services  
**Purpose**: Foundation for persistent, multi-server applications

**Use when you need:**
- ✅ **Multiple Asterisk servers** management
- ✅ **Automatic reconnection** handling
- ✅ **Event monitoring** infrastructure
- ✅ **Background services** foundation
- ✅ **Reusable service architecture**
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
## ✨ Key Features

### 🔧 Core AMI Client Features
* **Lightweight AMI client** for temporary connections
* **High-level abstraction** for sending Manager Actions (Originate, Hangup, Status, etc.)
* **Automatic connection management** with proper disposal patterns
* **Flexible connection options** (keepAlive configurable)
* **Comprehensive error handling** and logging

### 🏗️ Multi-Provider Service Features  
* **Multiple provider management** with automatic lifecycle handling
* **Automatic reconnection** with configurable retry logic
* **Event handling infrastructure** for real-time monitoring
* **Health monitoring** and status reporting
* **Connection state management** and cleanup
* **Extensible architecture** for custom implementations

### 🎯 **NEW: Smart Event Building & Logging**
* **🆕 Intelligent unknown event detection** with helpful diagnostic information
* **🆕 Smart logging deduplication** - no more log spam for repeated unknown events
* **🆕 Custom UserEvent registration** with runtime discovery
* **🆕 Performance optimized event parsing** with cached constructors
* **🆕 Diagnostic methods** for monitoring unknown events and registration status

### 🌐 Framework Support
* **Multi-target framework support** (.NET Standard 2.0, .NET 6-9)
* **ASP.NET Core integration** (through derived implementations)
* **Dependency injection** ready
* **Modern async/await** patterns throughout

## 🚀 Quick Start

### 📦 Installationdotnet add package Sufficit.Asterisk.Manager
### ⚡ Quick Operations// Configure provider
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
### 🏗️ Multi-Provider Service// Inherit from base class
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
## 🆕 **NEW: Smart Event Management**

### Custom UserEvent Registration// Define custom UserEvent
public class DoQueueStatusUserEvent : UserEvent
{
    public string? QueueName { get; set; }
    public string? Status { get; set; }
    public string? Agent { get; set; }
}

// Register at runtime
ManagerEventBuilder.RegisterUserEventClass(typeof(DoQueueStatusUserEvent));

// Now your custom events will be automatically detected and instantiated
### Event Diagnostics and Monitoring// Check if an event type is registered
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
### Smart Logging Benefits
The new smart logging system provides:

- **🎯 Intelligent Event Classification**: Distinguishes between UserEvents and standard events
- **📊 Log Level Optimization**: First occurrence gets detailed logging, subsequent ones use trace level
- **🔍 Helpful Registration Hints**: Provides exact code to register missing UserEvents
- **🚀 Performance Optimized**: Thread-safe deduplication with minimal overhead

Example smart log output:info: Unknown UserEvent 'DoQueueStatus' (key: userdoqueuestatus) - consider registering a custom class
info: To register this UserEvent, create a class inheriting from UserEvent and call: ManagerEventBuilder.RegisterUserEventClass(typeof(YourCustomEvent))
## 📊 Performance Comparison

| Approach | Connection Overhead | Memory Usage | Event Support | Smart Logging | Best For |
|----------|-------------------|--------------|---------------|---------------|----------|
| **AsteriskManagerProvider** | Per operation | Low | ❌ No | ✅ Yes | Scripts, one-time ops |
| **AsteriskManagerService** | None (persistent) | Medium | ✅ Real-time | ✅ Yes | Multi-server services |

## 🏗️ Architecture Overview┌─────────────────────────────────────────────────────────────────┐
│                    Your Application                             │
├─────────────────┬───────────────────────┬─────────────────────┤
│ Quick Operations│                       │ Multi-Provider Svc   │
│                 │                       │                     │
│ ┌─────────────┐ │                       │ ┌─────────────────┐ │
│ │AsteriskMgr  │ │  Sufficit.Asterisk   │ │AsteriskMgrSvc   │ │
│ │Provider     │ │     .Manager         │ │Base             │ │
│ └─────────────┘ │                       │ └─────────────────┘ │
├─────────────────┼───────────────────────┼─────────────────────┤
│        ManagerConnection                │        Multiple     │
│        (Single connection)              │        Providers    │
├─────────────────┴───────────────────────┴─────────────────────┤
│              Smart Event Processing                            │
│           (ManagerEventBuilder with Smart Logging)            │
├───────────────────────────────────────────────────────────────┤
│                 Connection Management                          │
│              (Reconnection, Auth, Events)                     │
├───────────────────────────────────────────────────────────────┤
│                    Asterisk Servers                           │
└───────────────────────────────────────────────────────────────┘
## 📚 Documentation

- 📖 **[Multi-Provider Architecture Guide](docs/README-MultiProviderArchitecture.md)** - Detailed architecture documentation
- 📖 **[Quick Operations Guide](docs/README-QuickOperations.md)** - AsteriskManagerProvider examples
- 📖 **[Smart Logging Guide](SMART_LOGGING.md)** - 🆕 NEW: Smart event logging and diagnostics
- 📖 **[API Reference](docs/README-API.md)** - Complete API documentation
- 📖 **[Migration Guide](docs/README-Migration.md)** - Upgrading from previous versions

## 🔧 Configuration

### Basic Provider Configuration{
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
### Multi-Provider Configuration{
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
### 🆕 Smart Logging Configuration{
  "Logging": {
    "LogLevel": {
      "Sufficit.Asterisk.Manager.ManagerEventBuilder": "Information"
    }
  }
}
## 🎯 Common Use Cases

### Scripts and Automation// Perfect for scheduled tasks, admin scripts
using var provider = new AsteriskManagerProvider(options, logger);
using var connection = await provider.ConnectAsync(keepAlive: false);

// Execute administrative commands
await connection.SendActionAsync(new ReloadAction());
await connection.SendActionAsync(new CoreShowChannelsAction());
### Call Center Applications// Perfect for real-time monitoring
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
### PBX Management Tools// Perfect for configuration management
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
## 🆕 **Recent Improvements**

### Version 1.2.0+ Features:
- ✅ **Smart Event Logging** - Intelligent unknown event detection with deduplication
- ✅ **Enhanced Diagnostics** - Runtime event registration monitoring  
- ✅ **Performance Optimizations** - Cached constructors and minimal allocations
- ✅ **UserEvent Support** - Runtime registration of custom user events
- ✅ **Thread-Safe Operations** - All event building operations are thread-safe
- ✅ **Memory Optimizations** - Reduced allocations in event processing pipeline

### Migration Notes:
- All existing code continues to work without changes
- New diagnostic methods are available for monitoring
- Smart logging is enabled by default
- UserEvent registration is now possible at runtime

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the [MIT License](LICENSE).

## 🆘 Support

- 📖 **Documentation**: [docs/](docs/)
- 🐛 **Issues**: [GitHub Issues](https://github.com/sufficit/sufficit-asterisk-manager/issues)  
- 💬 **Discussions**: [GitHub Discussions](https://github.com/sufficit/sufficit-asterisk-manager/discussions)
- 📧 **Email**: support@sufficit.com.br

---

**Made with ❤️ by the Sufficit Team**