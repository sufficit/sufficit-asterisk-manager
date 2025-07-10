# Sufficit.Asterisk.Manager

[![NuGet](https://img.shields.io/nuget/v/Sufficit.Asterisk.Manager.svg)](https://www.nuget.org/packages/Sufficit.Asterisk.Manager/)

## Description

`Sufficit.Asterisk.Manager` provides comprehensive **Asterisk Manager Interface (AMI)** integration for .NET applications, supporting both **quick operations** and **persistent multi-provider services**. This library offers dual-pattern architecture for different use cases: temporary connections for specific operations and persistent services for multi-server applications with automatic reconnection and event monitoring.

## Features

### Core AMI Client Features
- **Lightweight AMI client** for temporary connections
- **High-level abstraction** for sending Manager Actions (Originate, Hangup, Status, etc.)
- **Automatic connection management** with proper disposal patterns
- **Flexible connection options** (keepAlive configurable)
- **Comprehensive error handling** and logging

### Multi-Provider Service Features
- **Multiple provider management** with automatic lifecycle handling
- **Automatic reconnection** with configurable retry logic
- **Event handling infrastructure** for real-time monitoring
- **Health monitoring** and status reporting
- **Connection state management** and cleanup
- **Extensible architecture** for custom implementations

### Smart Event Management (NEW)
- **Intelligent unknown event detection** with helpful diagnostic information
- **Smart logging deduplication** - no more log spam for repeated unknown events
- **Custom UserEvent registration** with runtime discovery
- **Performance optimized event parsing** with cached constructors
- **Diagnostic methods** for monitoring unknown events and registration status

### Framework Support
- **Multi-target framework support** (.NET Standard 2.0, .NET 6-9)
- **ASP.NET Core integration** (through derived implementations)
- **Dependency injection** ready
- **Modern async/await** patterns throughout

## Installation
dotnet add package Sufficit.Asterisk.Manager
## Usage

For detailed usage examples and documentation, see [USAGE.md](USAGE.md).

## License

This project is licensed under the [MIT License](LICENSE).

## References and Thanks

This project builds upon the excellent foundation provided by several open-source Asterisk .NET libraries. We extend our heartfelt gratitude to the original authors and contributors:

### Reference Projects

- **[Asterisk.NET by roblthegreat](https://github.com/roblthegreat/Asterisk.NET)** - One of the original Asterisk .NET implementations that provided crucial insights into AMI protocol implementation, connection management, and event handling patterns.

- **[AsterNET by AsterNET Team](https://github.com/AsterNET/AsterNET)** - A comprehensive library that served as an excellent reference for AMI action implementations, event parsing strategies, and authentication mechanisms.

These pioneering projects were instrumental in understanding AMI protocol nuances, connection lifecycle management, and best practices for .NET integration with Asterisk. Our implementation builds upon this knowledge while introducing modern patterns and enterprise-grade reliability.

**Made with ❤️ by the Sufficit Team**