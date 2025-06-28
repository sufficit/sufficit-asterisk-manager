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

/*
 * =================================================================================================
 * CRITICAL PLATFORM COMPATIBILITY NOTE
 * =================================================================================================
 * The Connect method in this class has been specifically rewritten to use the `Socket` class
 * directly, bypassing `TcpClient`. This change is critical to resolve a persistent
 * `System.PlatformNotSupportedException` ("Sockets on this platform are invalid for use after a
 * failed connection attempt") that occurs on certain restrictive network environments or OS platforms.
 *
 * The root cause of the exception is the default .NET behavior where a single socket object might be
 * reused for multiple internal connection attempts (e.g., trying an IPv6 address, failing, and then
 * immediately retrying with an IPv4 address on the same socket).
 *
 * This implementation avoids the issue by:
 * 1. Manually resolving the host's IP addresses.
 * 2. Iterating through each IP address.
 * 3. Creating a brand new, clean `Socket` object for every single connection attempt.
 * 4. Immediately disposing of the socket if that specific attempt fails.
 * This ensures maximum control over the socket lifecycle and guarantees compatibility.
 * =================================================================================================
*/

using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
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
        private CancellationTokenSource? _managerCts;
        private readonly object _connectionLock = new object();

#if !NETSTANDARD
        [MemberNotNullWhen(true, nameof(_socket))]
#endif
        /// <inheritdoc cref="ISocketStatus.IsConnected"/>
        public bool IsConnected => _socket?.IsConnected ?? false;

        /// <inheritdoc cref="ISocketStatus.TotalBytesReceived"/>
        public ulong TotalBytesReceived => _socket?.TotalBytesReceived ?? 0;

        public event EventHandler<string>? OnConnectionIdentified;
        public event EventHandler<DisconnectEventArgs>? OnDisconnected;
        public event EventHandler<IDictionary<string, string>>? OnPacketReceived;

        public AMISocketManager (ManagerConnectionParameters parameters)
        {
            _parameters = parameters;
        }

        public async Task<bool> Connect (CancellationToken cancellationToken)
        {
            lock (_connectionLock)
            {
                if (IsConnected || IsDisposed) return IsConnected;
            }

            _logger.LogDebug("attempting to connect to Asterisk server {Hostname}:{Port}...", _parameters.Address, _parameters.Port);

            _managerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                // Step 1: Manually resolve the hostname to IP addresses.
                var addresses = await Dns.GetHostAddressesAsync(_parameters.Address);
                if (addresses == null || addresses.Length == 0)
                {
                    throw new SocketException((int)SocketError.HostNotFound);
                }

                // Step 2: Filter for IPv4 if the parameter is set.
                IEnumerable<IPAddress> targetAddresses = addresses;
                if (_parameters.ForceIPv4)
                {
                    targetAddresses = addresses.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                    if (!targetAddresses.Any())
                    {
                        _logger.LogWarning("ForceIPv4 is true, but no IPv4 address found for host {Hostname}.", _parameters.Address);
                        return false;
                    }
                }

                // Step 3: Loop through each address and attempt to connect.
                Socket? connectedSocket = null;
                foreach (var ipAddress in targetAddresses)
                {
                    // For each attempt, create a brand new socket. This is the core of the fix.
                    var socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        _logger.LogDebug("attempting to connect to IP {IPAddress} ...", ipAddress);

                        // Set socket options before connecting.
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                        // =================== INÍCIO DA MUDANÇA DE COMPATIBILIDADE ===================
                        // Use the ConnectAsync overload without a CancellationToken, which is netstandard2.0 compatible.
                        var connectTask = socket.ConnectAsync(new IPEndPoint(ipAddress, (int)_parameters.Port));

                        // Manually implement the timeout by racing the connect task against a delay task.
                        var delayTask = Task.Delay(TimeSpan.FromSeconds(10), _managerCts.Token);

                        var completedTask = await Task.WhenAny(connectTask, delayTask);

                        if (completedTask == delayTask)
                        {
                            // If the delay task completed first, it's a timeout.
                            throw new TimeoutException($"Connection to IP {ipAddress} timed out.");
                        }

                        // If the connectTask completed first, re-await it to propagate any potential exceptions.
                        await connectTask;
                        // =================== FIM DA MUDANÇA DE COMPATIBILIDADE ===================

                        if (socket.Connected)
                        {
                            _logger.LogDebug("successfully connected to IP: {IPAddress}.", ipAddress);
                            connectedSocket = socket;
                            break; // Exit the loop on the first successful connection.
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "failed to connect to IP: {IPAddress}. trying next address if available.", ipAddress);
                        // IMPORTANT: Immediately dispose of the failed socket.
                        socket.Dispose();
                    }
                }

                if (connectedSocket == null)
                {
                    _logger.LogError("could not connect to any of the resolved IP addresses for host {Hostname}.", _parameters.Address);
                    return false;
                }

                var options = new AGISocketOptions { Encoding = _parameters.SocketEncoding };
                _socket = new AISingleSocketHandler(_logger, options, connectedSocket, _managerCts.Token);
                _socket.OnDisconnected += OnSocketDisconnected;

                _ = ProcessSocketQueueAsync(_managerCts.Token);

                _logger.LogDebug("successfully connected to {Address}:{Port}. waiting for protocol identification ...", _parameters.Address, _parameters.Port);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "critical error occurred during the connection process to {Address}:{Port}.", _parameters.Address, _parameters.Port);
                return false;
            }
        }

        /// <summary>
        /// This task replaces the old ManagerReader. It consumes lines from the socket handler,
        /// assembles them into packets, and dispatches them to the application.
        /// </summary>
        private async Task ProcessSocketQueueAsync (CancellationToken cancellationToken)
        {
            if (_socket == null)
            {
                _logger.LogWarning("ProcessSocketQueueAsync started but the socket is null. Aborting task.");
                return;
            }

            _logger.LogTrace("packet processing queue consumer started.");
            var packet = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var commandList = new List<string>();
            bool isProcessingCommandResult = false;
            bool isWaitingForIdentifier = true;

            try
            {
                // It will now yield the thread while waiting for new lines instead of blocking it.
                await foreach (var line in _socket.ReadQueue(cancellationToken))
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
                _logger.LogTrace("packet processing was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "packet processing queue encountered an unhandled exception");
            }
            finally
            {
                _logger.LogDebug("packet processing queue consumer finished");
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

                if (cause.Contains("disposed"))
                    _logger.LogTrace("disconnecting from Asterisk server. cause: {cause}", cause);
                else
                    _logger.LogInformation("disconnecting from Asterisk server. cause: {cause}", cause);

                // Dispose the socket handler, which will stop the background tasks.
                _socket?.Dispose();
                _socket = null;
            }

            var args = new DisconnectEventArgs() { Cause = cause, IsPermanent = isPermanent };
            OnDisconnectedTrigger(args);
        }

        protected virtual void OnDisconnectedTrigger(DisconnectEventArgs args)
            => OnDisconnected?.Invoke(this, args);

        public async Task WriteAsync (string data, CancellationToken cancellationToken)
        {
            if (!IsConnected)
                throw new NotConnectedException("Cannot write data, socket is not connected.");

            _logger.LogTrace("Writing to socket: \n{data}", data);
            await _socket.WriteAsync(data, cancellationToken);
        }

        #region DISPOSABLE

        public bool IsDisposed { get; private set; }

        public virtual void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;

            // cancel any operations and disconnect
            if (_managerCts != null)
            {
                if (!_managerCts.IsCancellationRequested)
                    _managerCts.Cancel();

                _managerCts.Dispose();
            }

            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }

            Disconnect($"{nameof(AMISocketManager)} disposed", isPermanent: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}