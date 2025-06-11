using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager.Connection
{
    /// <summary>
    /// Asterisk Manager Interface Socket Life Cycle Manager
    /// </summary>
    public interface IAMISocketManager
    {
        bool IsConnected { get; }

        Task<bool> Connect(CancellationToken cancellationToken);

        void Disconnect(string cause, bool isPermanent = false);

        event EventHandler<string>? OnConnectionIdentified;

        event EventHandler<DisconnectEventArgs>? OnDisconnected;

        event EventHandler<IDictionary<string, string>>? OnPacketReceived;
    }
}
