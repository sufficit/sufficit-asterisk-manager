/*
 * =================================================================================================
 * REFACTORING SUMMARY
 * =================================================================================================
 * This version of AMISocketManager has been refactored to work directly with the new,
 * active AISingleSocketHandler.
 *
 * KEY CHANGES:
 * - The separate `ManagerReader` class has been completely removed to solve the
 * "two readers" conflict.
 * - This class now contains a consumer task (`ProcessSocketQueueAsync`) that reads lines
 * directly from the AISingleSocketHandler's internal queue.
 * - This new design is simpler, more direct, and correctly integrates the efficient,
 * async socket handler with the application logic.
 * =================================================================================================
*/

using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.IO;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager.Connection
{
    /// <summary>
    /// Manages the physical connection to the Asterisk server. It now directly consumes
    /// data from the active socket handler, orchestrating the parsing and dispatching of packets.
    /// </summary>
    public class AMISocketManager : IAMISocketManager, IDisposable
    {
        private static readonly ILogger _logger = ManagerLogger.CreateLogger<AMISocketManager>();

        private readonly ManagerConnectionParameters _parameters;
        private AISingleSocketHandler? _socket; // Changed to the concrete, active type
        private readonly CancellationTokenSource _managerCts;
        private readonly object _connectionLock = new object();

        public bool IsConnected => _socket?.IsConnected ?? false;

        public event EventHandler<string>? OnConnectionIdentified;
        public event EventHandler<DisconnectEventArgs>? OnDisconnected;
        public event EventHandler<IDictionary<string, string>>? OnPacketReceived;

        public AMISocketManager(ManagerConnectionParameters parameters)
        {
            _parameters = parameters;
            _managerCts = new CancellationTokenSource();
        }

        public async Task<bool> Connect(CancellationToken cancellationToken)
        {
            lock (_connectionLock)
            {
                if (IsConnected || IsDisposed) return IsConnected;
            }

            _logger.LogInformation("Attempting to connect to Asterisk server {Hostname}:{Port}...", _parameters.Address, _parameters.Port);
            try
            {
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _managerCts.Token);
                var tcpClient = new TcpClient();

                // These settings will make the OS send a probe after 60 seconds of inactivity,
                // and then send 5 more probes every 5 seconds before considering the connection dead.
                tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                // The following options might not be available on all OSes (e.g., older macOS)
                // but are safe to set on Linux and Windows.
                // tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 60);
                // tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 5);
                // tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 5);

                var connectTask = tcpClient.ConnectAsync(_parameters.Address, (int)_parameters.Port);
                var delayTask = Task.Delay(TimeSpan.FromSeconds(10), linkedCts.Token);

                var completedTask = await Task.WhenAny(connectTask, delayTask);

                if (completedTask == delayTask)
                {
                    // If the delay task finished first, it's a timeout.
                    throw new TimeoutException($"Connection to {_parameters.Address}:{_parameters.Port} timed out after 10 seconds.");
                }

                // If we get here, the connectTask finished successfully.
                // We should await it to propagate any potential connection exceptions.
                await connectTask;

                var options = new AGISocketOptions
                {
                    Encoding = _parameters.SocketEncoding,
                };

                // Create the active socket handler. It will start reading automatically.
                _socket = new AISingleSocketHandler(_logger, options, tcpClient.Client, linkedCts.Token);
                _socket.OnDisconnected += OnSocketDisconnected;

                // Start the task that will consume and process lines from the socket handler's queue.
                _ = ProcessSocketQueueAsync(linkedCts.Token);

                _logger.LogInformation("Successfully connected to {Address}:{Port}. Waiting for protocol identification...", _parameters.Address, _parameters.Port);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to {Address}:{Port}.", _parameters.Address, _parameters.Port);
                return false;
            }
        }

        /// <summary>
        /// This task replaces the old ManagerReader. It consumes lines from the socket handler,
        /// assembles them into packets, and dispatches them to the application.
        /// </summary>
        private async Task ProcessSocketQueueAsync(CancellationToken token)
        {
            if (_socket == null)
            {
                _logger.LogWarning("ProcessSocketQueueAsync started but the socket is null. Aborting task.");
                return;
            }

            _logger.LogInformation("Packet processing queue consumer started.");
            var packet = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var commandList = new List<string>();
            bool isProcessingCommandResult = false;
            bool isWaitingForIdentifier = true;

            try
            {
                // It will now yield the thread while waiting for new lines instead of blocking it.
                await foreach (var line in _socket.ReadQueue(token))
                {
                    // The logic inside the loop remains exactly the same as before.
                    if (line == null) continue;

                    if (isWaitingForIdentifier)
                    {
                        if (line.StartsWith("Asterisk Call Manager", StringComparison.OrdinalIgnoreCase))
                        {
                            isWaitingForIdentifier = false;
                            HandleConnectionIdentified(line);
                        }
                        continue;
                    }

                    if (isProcessingCommandResult)
                    {
                        if (line == "--END COMMAND--")
                        {
                            isProcessingCommandResult = false;
                            packet["output"] = string.Join("\n", commandList);
                            commandList.Clear();
                        }
                        else
                        {
                            commandList.Add(line);
                        }
                    }
                    else if (!string.IsNullOrEmpty(line))
                    {
                        packet.AddKeyValue(line);
                        if (line.StartsWith("Response: Follows", StringComparison.OrdinalIgnoreCase))
                        {
                            isProcessingCommandResult = true;
                            commandList.Clear();
                        }
                    }
                    else // An empty line marks the end of a packet.
                    {
                        if (packet.Count > 0)
                        {
                            // Dispatch a copy of the packet
                            HandlePacketReceived(new Dictionary<string, string>(packet, StringComparer.OrdinalIgnoreCase));
                            packet.Clear();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace("Packet processing was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Packet processing queue encountered an unhandled exception.");
            }
            finally
            {
                _logger.LogInformation("Packet processing queue consumer finished.");
            }
        }

        private void OnSocketDisconnected(object? sender, AGISocketReason e)
        {
            if (_socket != null)
                _socket.OnDisconnected -= OnSocketDisconnected;

            Disconnect("Socket disconnected: " + e.ToString());
        }

        /// <summary>
        /// Handles the receipt of a packet and triggers the <see cref="OnPacketReceived"/> event.
        /// </summary>
        /// <remarks>This method invokes the <see cref="OnPacketReceived"/> event, passing the current
        /// instance and the received packet as arguments. Derived classes can override this method to provide custom
        /// handling for received packets.</remarks>
        /// <param name="packet">A dictionary containing the packet data, where keys represent field names and values represent field values.</param>
        protected virtual void HandlePacketReceived(IDictionary<string, string> packet)
        {
            OnPacketReceived?.Invoke(this, packet);
        }

        private void HandleConnectionIdentified(string protocolIdentifier)
        {
            OnConnectionIdentified?.Invoke(this, protocolIdentifier);
        }

        public virtual void Disconnect(string cause, bool isPermanent = false)
        {
            lock (_connectionLock)
            {
                if (!IsConnected && !IsDisposed) return;

                _logger.LogInformation("Disconnecting from Asterisk server. Cause: {Cause}", cause);

                // Dispose the socket handler, which will stop the background tasks.
                _socket?.Dispose();
                _socket = null;
            }

            var args = new DisconnectEventArgs() { Cause = cause, IsPermanent = isPermanent };
            OnDisconnectedTrigger(args);
        }

        protected virtual void OnDisconnectedTrigger(DisconnectEventArgs args)
            => OnDisconnected?.Invoke(this, args);

        public void Write(string data)
        {
            if (!IsConnected || _socket == null)
                throw new NotConnectedException("Cannot write data, socket is not connected.");

            _logger.LogTrace("Writing to socket: \n{data}", data);
            _socket.Write(data);
        }

        #region DISPOSABLE

        public bool IsDisposed { get; private set; }

        public virtual void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;

            // Cancel any operations and disconnect
            if (!_managerCts.IsCancellationRequested)
                _managerCts.Cancel();

            Disconnect("Manager disposed", isPermanent: true);
            _managerCts.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}