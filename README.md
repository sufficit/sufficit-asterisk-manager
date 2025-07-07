<h1>
  Sufficit.Asterisk.Manager
  <a href="https://github.com/sufficit"><img src="https://avatars.githubusercontent.com/u/66928451?s=200&v=4" alt="Sufficit Logo" width="80" align="right"></a>
</h1>

[![NuGet](https://img.shields.io/nuget/v/Sufficit.Asterisk.Manager.svg)](https://www.nuget.org/packages/Sufficit.Asterisk.Manager/)

## 📖 About the Project

`Sufficit.Asterisk.Manager` is a module responsible for communicating with the **Asterisk Manager Interface (AMI)**. It provides an abstraction layer for sending actions to Asterisk and receiving events and responses, facilitating the management and integration of the telephony server from the Sufficit platform.

### ✨ Key Features

* Secure and persistent connection to the AMI.
* High-level abstraction for sending `Manager Actions` (e.g., Originate, Hangup, Status).
* Parsing and handling of `Manager Events` (e.g., Newchannel, Hangup, PeerStatus).
* Real-time state management of calls and channels.

## 🚀 Getting Started

To use this project, you will need a .NET environment and access to an Asterisk server with AMI enabled.

### 📋 Prerequisites

* .NET SDK (e.g., .NET 6.0 or higher)
* An Asterisk server with a configured AMI user.

### 📦 NuGet Package

Install the package into your project via the .NET CLI or the NuGet Package Manager Console.

**.NET CLI:**

    dotnet add package Sufficit.Asterisk.Manager

**Package Manager Console:**

    Install-Package Sufficit.Asterisk.Manager

## 🛠️ Usage

This module can be run as a background service or integrated into a larger application.

**Example of how to start the connection and send an action:**

    using Sufficit.Asterisk.Manager;
    using Sufficit.Asterisk.Manager.Actions;

    // Create connection
    var connection = new ManagerConnection();
    
    // Connect to Asterisk
    await connection.LoginAsync("127.0.0.1", 5038, "admin", "mysecretpassword");

    // Example of sending an action
    var originateAction = new OriginateAction
    {
        Channel = "SIP/1000",
        Context = "default",
        Exten = "1001",
        Priority = 1
    };

    ManagerResponse response = await connection.SendActionAsync(originateAction);
    Console.WriteLine($"Originate response: {response.Message}");

    // Subscribe to events
    connection.ManagerEvent += (sender, e) =>
    {
        Console.WriteLine($"Event received: {e.EventName}");
    };

    // Disconnect when done
    await connection.LogoffAsync();

## 🤝 Contributing

Your help is essential for the growth of this project. Feel free to open issues and pull requests.

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.

## 📧 Contact

Sufficit - [contato@sufficit.com.br](mailto:contato@sufficit.com.br)

Project Link: [https://github.com/sufficit/sufficit-asterisk-manager](https://github.com/sufficit/sufficit-asterisk-manager)