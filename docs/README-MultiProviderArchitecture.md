# Multi-Provider AMI Service Architecture

## Overview

This document describes the refactored architecture that separates the core multi-provider functionality (in the public `sufficit-asterisk-manager` library) from the business-specific implementation (in the private `sufficit-ami-events` project).

## ??? Architecture Separation

### ?? Public Library: `sufficit-asterisk-manager`

**Location**: `sufficit-asterisk-manager/src/Services/`

**Purpose**: Provides the foundational infrastructure for managing multiple Asterisk connections with automatic reconnection.

**Key Components**:
- `AsteriskManagerService` - Abstract base class with core functionality
- `IAsteriskManagerService` - Interface defining the service contract

**What it provides**:
- ? Multiple provider management and lifecycle
- ? Automatic connection establishment with configurable retry logic  
- ? Connection state management and cleanup
- ? Basic health monitoring
- ? Event handling infrastructure
- ? Service lifecycle management

**What it does NOT include**:
- ? ASP.NET Core dependencies (BackgroundService, IHealthCheck)
- ? Business-specific logic or commands
- ? SignalR integration
- ? Application-specific configurations

### ?? Private Implementation: `sufficit-ami-events`

**Location**: `sufficit-ami-events/src/Services/AMIService.cs`

**Purpose**: Implements the business-specific logic for the AMI Events application.

**What it adds**:
- ? **Direct inheritance** from AsteriskManagerService
- ? IHostedService and IHealthCheck implementations for ASP.NET Core
- ? SignalR event forwarding
- ? Business-specific event handling
- ? Custom configuration management
- ? **Business-specific commands** (CoreShowChannels, GetQueueStatus, SIPReload, etc.)
- ? Application-specific logic

## ?? Benefits of This Architecture

### For the Public Library (`sufficit-asterisk-manager`)
- **?? Reusable**: Other projects can use the same multi-provider infrastructure
- **?? Focused**: Contains only essential connection management functionality
- **?? Lightweight**: No unnecessary dependencies or business logic
- **?? Single Responsibility**: Handles only connection lifecycle and basic operations
- **?? Testable**: Core logic can be tested independently

### For the Private Implementation (`sufficit-ami-events`)
- **?? Business-Focused**: Contains only application-specific logic and commands
- **?? Integration-Ready**: Full ASP.NET Core integration via direct inheritance
- **?? Extensible**: Easy to add new business features and commands
- **?? Customizable**: Can override base functionality as needed
- **?? Maintainable**: Clear separation of concerns with simple inheritance

## ?? Class Hierarchy
AsteriskManagerService (sufficit-asterisk-manager)
??? Abstract base class
??? Core multi-provider functionality
??? Connection management and lifecycle
??? Basic health checking
??? Event handling infrastructure
??? NO business-specific commands

AMIService (sufficit-ami-events)
??? Inherits directly from AsteriskManagerService
??? Implements IHostedService + IHealthCheck
??? Adds ALL business-specific commands
??? Integrates with ASP.NET Core
??? Contains SignalR and other business logic
??? Simple inheritance pattern (no composition)
## ?? Usage Examples

### Creating a Custom Multi-Provider Service
// In your project - inherit from the public base class
public class MyAsteriskService : AsteriskManagerService
{
    public override AsteriskManagerEvents Events { get; }
    
    public MyAsteriskService(ILoggerFactory loggerFactory) : base(loggerFactory)
    {
        Events = GetEventHandler();
    }
    
    protected override AsteriskManagerEvents GetEventHandler()
    {
        var events = new AsteriskManagerEvents();
        // Configure your specific event handling
        events.On<NewChannelEvent>(OnNewChannel);
        return events;
    }
    
    protected override IEnumerable<AMIProviderOptions> GetProviderConfigurations()
    {
        // Return your provider configurations
        return myConfigurations;
    }
    
    private void OnNewChannel(object? sender, NewChannelEvent evt)
    {
        // Your business logic here
    }
    
    // Add your own business-specific commands
    public async Task MyCustomCommand()
    {
        foreach (var provider in Providers)
        {
            if (provider.Connection != null)
            {
                await provider.Connection.SendActionAsync(new MyCustomAction());
            }
        }
    }
}
### Using as Background Service (like AMIService does)
public class MyBackgroundAMIService : AsteriskManagerService, IHostedService
{
    public MyBackgroundAMIService(ILoggerFactory factory) : base(factory)
    {
        Events = GetEventHandler();
    }
    
    // Implement abstract methods
    protected override AsteriskManagerEvents GetEventHandler() { /* ... */ }
    protected override IEnumerable<AMIProviderOptions> GetProviderConfigurations() { /* ... */ }
    
    // Implement IHostedService
    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        await StartAsync(cancellationToken);
    }
    
    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        await StopAsync(cancellationToken);
    }
    
    // Add your business commands here
    public async Task GetSystemStatus()
    {
        var provider = Providers.FirstOrDefault();
        if (provider?.Connection != null)
        {
            await provider.Connection.SendActionAsync(new CoreShowChannelsAction());
        }
    }
}
## ?? Key Methods in AsteriskManagerService

### Core Service ManagementTask StartAsync(CancellationToken cancellationToken)          // Starts all providers
Task StopAsync(CancellationToken cancellationToken)           // Stops all providers  
Task RunUntilCancellationAsync(CancellationToken cancellationToken) // Runs until cancelled
Task ReloadAsync(CancellationToken cancellationToken)         // Reloads configuration
### Health and Status(bool IsHealthy, string Status) CheckHealth()                 // Simple health check
DateTimeOffset LastReceivedEvent { get; }                     // Last event timestamp
ICollection<AsteriskManagerProvider> Providers { get; }       // Provider collection
### Abstract Methods (Must Override)protected abstract AsteriskManagerEvents GetEventHandler()           // Event configuration
protected abstract IEnumerable<AMIProviderOptions> GetProviderConfigurations() // Provider config
### Virtual Methods (Can Override)protected virtual string ServiceName                          // Service name for logging
protected virtual void OnManagerEventReceived(...)            // Event handling hook
protected virtual AsteriskManagerProvider CreateProvider(...) // Custom provider creation
protected virtual HashSet<AsteriskManagerProvider> LoadProviders(...) // Provider loading
## ?? Command Separation Strategy

### ? NOT in AsteriskManagerService (Public Library)// These methods are NOT in the public library:
// Task CoreShowChannels(CancellationToken cancellationToken)
// Task Refresh(CancellationToken cancellationToken)  
// Task GetQueueStatus(string queue, string member, ...)
// Task GetSIPRegistrations(CancellationToken cancellationToken)
// Task SIPReload(CancellationToken cancellationToken)
### ? IN AMIService (Business Implementation)// These methods are in the business implementation:
public async Task CoreShowChannels(CancellationToken cancellationToken)
public async Task Refresh(CancellationToken cancellationToken = default)
public async Task GetQueueStatus(string queue, string member, ...)
public async Task GetSIPRegistrations(CancellationToken cancellationToken = default)
public async Task SIPReload(CancellationToken cancellationToken = default)
public async void SendAction(string providerTitle, string queue, ...)
## ?? Migration Guide

### 1. For Public Libraries
- Inherit from `AsteriskManagerService`
- Implement required abstract methods
- Add your own business commands as needed
- Focus on core functionality only

### 2. For Private/Business Applications  
- **Use direct inheritance** from `AsteriskManagerService`
- Implement `IHostedService` if needed for ASP.NET Core
- Add business-specific commands in your implementation
- All AMI commands are now application-specific

### 3. Configuration
- Provider configurations remain the same
- Health check integration available but optional
- Event handling can be customized per application
- Command implementation is now application-specific

## ?? AMIService Simplified Architecture

The new AMIService uses **direct inheritance** instead of composition:
public class AMIService : AsteriskManagerService, IHostedService, IAMIService, IHealthCheck
{
    // Direct inheritance - no composition needed
    public override AsteriskManagerEvents Events { get; }
    
    // Implement abstract methods from base class
    protected override AsteriskManagerEvents GetEventHandler() { /* business logic */ }
    protected override IEnumerable<AMIProviderOptions> GetProviderConfigurations() { /* config */ }
    
    // Implement IHostedService for ASP.NET Core
    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        await StartAsync(cancellationToken); // Calls base class method
    }
    
    // Add business commands directly
    public async Task CoreShowChannels(CancellationToken cancellationToken) { /* business logic */ }
}
## ?? Performance Characteristics

The simplified architecture maintains the same performance benefits:

- **Persistent Connections**: No connection overhead per operation
- **Automatic Reconnection**: Resilient to network issues  
- **Multi-Server Support**: Concurrent connections to multiple Asterisk servers
- **Event Streaming**: Real-time event processing
- **Resource Management**: Proper disposal and cleanup
- **Simplified Memory Usage**: No composition overhead

## ?? Key Benefits Summary

1. **?? Public Library is Cleaner**: No business logic, just infrastructure
2. **?? Simple Inheritance**: AMIService directly inherits from base class
3. **?? Better Reusability**: Other projects can build their own command sets
4. **?? Single Responsibility**: Each class has a clear, focused purpose
5. **?? Better Testability**: Infrastructure and business logic can be tested separately
6. **? Simplified Architecture**: No unnecessary composition layers

---

This refactoring provides a **truly reusable** foundation with **simple inheritance patterns** for building AMI-based applications! ??