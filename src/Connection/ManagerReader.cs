using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager.Connection
{
    /// <summary>
    /// Reads data from an ISocketConnection and parses it into AMI packets.
    /// This class is decoupled from ManagerConnection and uses callbacks to report its findings,
    /// such as received packets or the initial protocol identifier.
    /// </summary>
    public class ManagerReader
    {
        private static readonly ILogger _logger = ManagerLogger.CreateLogger<ManagerReader>();

        // Callbacks to notify the owner of events
        private readonly Action<Dictionary<string, string>> _packetReceivedCallback;
        private readonly Action<string> _protocolIdentifiedCallback;

        /// <summary>
        ///     Gets the cause of the current condition or error.
        /// </summary>
        public string? Cause { get; private set; }

        public ManagerReader (Action<IDictionary<string, string>> packetReceivedCallback, Action<string> protocolIdentifiedCallback)
        {
            _packetReceivedCallback = packetReceivedCallback ?? throw new ArgumentNullException(nameof(packetReceivedCallback));
            _protocolIdentifiedCallback = protocolIdentifiedCallback ?? throw new ArgumentNullException(nameof(protocolIdentifiedCallback));
        }

        /// <summary>
        /// Main loop for reading data from the Asterisk Manager Interface.
        /// </summary>
        public async Task RunAsync (ISocketConnection socket, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ManagerReader async consumer loop started.");
            var lineBuffer = new StringBuilder();
            var packet = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var commandList = new List<string>();
            bool processingCommandResult = false;
            bool waitForIdentifier = true;
            Cause = null;

            try
            {
                var stream = socket.GetStream() ?? throw new InvalidOperationException("Could not get a readable network stream.");
                var buffer = new byte[(int)socket.Options.BufferSize];

                while (!cancellationToken.IsCancellationRequested && socket.IsConnected)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0)
                    {
                        _logger.LogWarning("Connection closed by peer (ReadAsync returned 0).");
                        Cause = "Connection closed by peer.";
                        break;
                    }

                    string receivedString = socket.Options.Encoding.GetString(buffer, 0, bytesRead);
                    lineBuffer.Append(receivedString);

                    // Process all complete lines in the buffer
                    ProcessLineBuffer(lineBuffer, packet, commandList, ref processingCommandResult, ref waitForIdentifier);
                }
            }
            catch (OperationCanceledException)
            {
                Cause = "ManagerReader loop was canceled.";
                _logger.LogInformation("ManagerReader loop was canceled.");
            }
            catch (IOException ex)
            {
                Cause = ex.Message;
                _logger.LogWarning(ex, "ManagerReader loop ending due to a connection-level exception.");
            }
            catch (Exception ex)
            {
                Cause = ex.Message;
                _logger.LogError(ex, "Fatal unhandled exception in ManagerReader.RunAsync loop.");
            }
            finally
            {
                Cause = "ManagerReader async consumer loop finished.";
                _logger.LogInformation("ManagerReader async consumer loop finished.");
            }
        }

        private void ProcessLineBuffer(StringBuilder lineBuffer, Dictionary<string, string> packet, List<string> commandList, ref bool processingCommandResult, ref bool waitForIdentifier)
        {
            while (true)
            {
                int newlineIndex = -1;
                for (int i = 0; i < lineBuffer.Length; i++)
                {
                    if (lineBuffer[i] == '\n') { newlineIndex = i; break; }
                }

                if (newlineIndex == -1) break;

                int lineLength = newlineIndex;
                if (lineLength > 0 && lineBuffer[lineLength - 1] == '\r') lineLength--;

                string singleLine = lineBuffer.ToString(0, lineLength);

                if (waitForIdentifier)
                {
                    if (singleLine.StartsWith("Asterisk Call Manager", StringComparison.OrdinalIgnoreCase))
                    {
                        waitForIdentifier = false;
                        _protocolIdentifiedCallback(singleLine);
                    }
                }
                else if (processingCommandResult)
                {
                    if (singleLine == "--END COMMAND--")
                    {
                        processingCommandResult = false;
                        packet["output"] = string.Join("\n", commandList);
                        commandList.Clear();
                    }
                    else
                    {
                        commandList.Add(singleLine);
                    }
                }
                else if (lineLength > 0)
                {
                    packet.AddKeyValue(singleLine);
                    if (singleLine.StartsWith("Response: Follows", StringComparison.OrdinalIgnoreCase))
                    {
                        processingCommandResult = true;
                        commandList.Clear();
                    }
                }
                else // Blank line: packet terminator
                {
                    if (packet.Count > 0)
                    {
                        // Dispatch a copy of the packet
                        _packetReceivedCallback(new Dictionary<string, string>(packet, StringComparer.OrdinalIgnoreCase));
                        packet.Clear();
                    }
                }
                lineBuffer.Remove(0, newlineIndex + 1);
            }
        }
    }
}
