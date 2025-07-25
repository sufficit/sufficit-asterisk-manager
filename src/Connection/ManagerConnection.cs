﻿using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.IO;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using Sufficit.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager.Connection
{
    /// <summary>
    /// Default implementation of the IManagerConnection interface.
    /// Manages a connection to the Asterisk Manager Interface, handling login, event dispatching, action sending,
    /// and robust auto-reconnection.
    /// </summary>
    public partial class ManagerConnection : ActionDispatcher, IManagerConnection, IDisposable
    {
        private static readonly ILogger _logger = ManagerLogger.CreateLogger<ManagerConnection>();

        /// <summary>
        /// Gets the array of characters used as delimiters for variable parsing.
        /// </summary>
        public char[] VarDelimiters { get; internal set; }

        /// <summary>
        ///     Indicates if it its the first attemp to login, used for auto discover the <see cref="VarDelimiters"/> if necessary
        /// </summary>
        private bool _isFirstLogin = true;

        /// <summary>
        ///     Auto discovered Asterisk Version, if requested
        /// </summary>
        public AsteriskVersion? AsteriskVersion { get; internal set; }

        bool IManagerConnection.KeepAlive => _parameters.KeepAlive;

        string IManagerConnection.Address => _parameters.Address;

        private readonly ManagerConnectionParameters _parameters;

        private volatile int _queueCounter = 0;
        private readonly Channel<IDictionary<string, string>> _packetChannel;
        private readonly Task _packetConsumerTask;

        #region Properties

        public bool IsAuthenticated => _authenticator.IsAuthenticated;
        public Encoding SocketEncoding => _parameters.SocketEncoding;

        /// <summary>
        /// Internal event manager - always exists and is owned by this connection.
        /// Disposed automatically when the connection is disposed.
        /// </summary>
        private readonly ManagerEventSubscriptions _internalEvents;
        
        /// <summary>
        /// External event manager - optional, provided via Use() method.
        /// Never disposed by this connection, managed externally.
        /// </summary>
        private IManagerEventSubscriptions? _externalEvents;
        
        /// <summary>
        /// Currently active event manager (either internal or external).
        /// </summary>
        private IManagerEventSubscriptions _activeEvents;

        /// <summary>
        /// Gets the currently active event management system for this connection.
        /// This will be either the internal events (default) or external events (if set via Use()).
        /// </summary>
        public IManagerEventSubscriptions Events => _activeEvents;

        #endregion

        #region CONSTRUCTORS

        private readonly ConnectionAuthenticator _authenticator;
        private readonly ConnectionLivenessMonitor _livenessMonitor;
        private readonly ConnectionReconnector _reconnector;

        public ManagerConnection (ManagerConnectionParameters parameters) : base(parameters)
        {
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

            // 1. Crie o Channel. Unbounded significa que a fila não tem limite.
            // Cuidado: em cenários extremos, isso pode consumir memória. 
            // Para mais controle, use `new BoundedChannelOptions(...)`.
            _packetChannel = Channel.CreateUnbounded<IDictionary<string, string>>(
                new UnboundedChannelOptions { SingleReader = true }); // Otimização para um único consumidor

            // 2. Inicie a tarefa do consumidor em segundo plano
            _packetConsumerTask = Task.Run(ProcessPacketQueueAsync);

            // 3. Create internal event manager - always owned by this connection
            _internalEvents = new ManagerEventSubscriptions();
            
            // 4. Start with internal events as active (external can be set via Use())
            _activeEvents = _internalEvents;

            // updating default delimeters
            VarDelimiters = this.GetDelimiters();

            // 5. Create the component that handles the login/logoff logic
            _authenticator = new ConnectionAuthenticator (parameters, this, this);
            _authenticator.OnAuthenticated = OnAuthenticated;

            // 6. Create the component that sends pings to keep the connection alive
            _livenessMonitor = new ConnectionLivenessMonitor (parameters, this, this);

            _reconnector = new ConnectionReconnector(parameters, this, _authenticator, _livenessMonitor);
            _reconnector.Start(); // Ativa o listener de desconexão
            
            _logger.LogDebug("ManagerConnection created with internal event manager");
        }

        #endregion
        #region CHANNELING PACKETS

        /// <remarks>Overriding for channeling packets in order to avoid thread and memory leaks</remarks>
        /// <inheritdoc cref="AMISocketManager.HandlePacketReceived(IDictionary{string, string})"/> 
        protected override void HandlePacketReceived(IDictionary<string, string> packet)
        {
            //_logger.LogTrace("on base packet received, count on channel: {count}, packet: {json}", _queueCounter, packet.ToJson());

            // updating timestamp for liveness monitoring
            _livenessMonitor.LastMessageReceived = DateTime.UtcNow;

            // A única tarefa aqui é tentar escrever na fila.
            // TryWrite é não-bloqueante e extremamente rápido.
            if (_packetChannel.Writer.TryWrite(packet))
            {
                // Incrementa o contador de forma segura para threads
                Interlocked.Increment(ref _queueCounter);
            }
            else
            {
                // Log de erro se, por algum motivo, não for possível escrever na fila.
                // Com uma fila Unbounded, isso é muito improvável.
                _logger.LogError("Failed to write packet to channel. The queue may be closed.");
            }
        }

        /// <summary>
        /// Processes packets from the queue asynchronously for the lifetime of the connection.
        /// </summary>
        /// <remarks>This method continuously reads packets from the internal channel until the channel is
        /// closed. Each packet is processed based on its content, such as dispatching events or handling responses.
        /// Unrecognized packets are logged as warnings, and any errors during processing are logged as
        /// errors.</remarks>
        /// <returns></returns>
        private async Task ProcessPacketQueueAsync()
        {
            // Este loop roda pela vida inteira da conexão, até que o Channel seja fechado.
            await foreach (var packet in _packetChannel.Reader.ReadAllAsync())
            {
                // Decrementa o contador de forma segura para threads
                Interlocked.Decrement(ref _queueCounter);

                try
                {
                    if (packet.ContainsKey("event"))
                    {
                        // Use the currently active events system (internal or external)
                        var currentEvents = _activeEvents;
                        if (currentEvents != null && !currentEvents.IsDisposed)
                        {
                            // Build event using ManagerEventBuilder static class
                            var eventObject = ManagerEventBuilder.Build(packet);
                            if (eventObject != null)
                            {
                                // Dispatch to the active events system
                                currentEvents.Dispatch(this, eventObject.Event);
                            }
                        }
                        else
                        {
                            // Log only at debug level during normal operation
                            // This can happen briefly during reconnection
                            var eventType = packet.TryGetValue("event", out var eventValue) ? eventValue : "Unknown";
                            _logger.LogDebug("Event packet received but active events system is disposed or null. Event type: {type}", eventType);
                        }
                    }
                    else if (packet.ContainsKey("response"))
                    {
                        DispatchResponse(packet);
                    }
                    else
                    {
                        _logger.LogWarning("unrecognized packet: {json}", packet.ToJson());
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Expected when events system is disposed during shutdown, log at debug level
                    _logger.LogDebug("Events system was disposed while processing packet");
                    break; // Exit the loop gracefully
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing packet from queue.");
                }
            }

            _logger.LogDebug("Packet processing loop has ended");
        }

        #endregion
        #region LOG IN & OFF & POST AUTHENTICATION

        /// <inheritdoc />
        public Task Login(CancellationToken cancellationToken)
            => _authenticator.Login(cancellationToken);

        /// <inheritdoc />
        public Task LogOff(CancellationToken cancellationToken = default)
            => _authenticator.LogOff(cancellationToken);
        
        /// <summary>
        /// Handles actions to be performed upon successful authentication.
        /// </summary>
        /// <remarks>This method is invoked after the authentication process completes. It performs
        /// initialization tasks such as determining the Asterisk version and updating internal delimiters. If this is
        /// the first login  attempt, additional setup is performed based on the provided parameters.</remarks>
        /// <returns></returns>
        private async ValueTask OnAuthenticated()
        {
            // is the first login attempt ?
            if (_isFirstLogin)
            {
                _isFirstLogin = false;

                // user don't know the asterisk version, we should try to discover
                if (!_parameters.OldVersion.HasValue)
                    AsteriskVersion = await this.GetAsteriskVersion(default);

                // user specified that is an older asterisk version
                else if (_parameters.OldVersion.Value)
                    AsteriskVersion = Asterisk.AsteriskVersion.ASTERISK_Older;

                // updating delimeters
                VarDelimiters = this.GetDelimiters();
            }
        }

        #endregion        
        #region SEND ACTION

        public override async Task SendActionAsync(ManagerAction action, IResponseHandler? responseHandler, CancellationToken cancellationToken)
        {
            if (!(action.Action.Equals("login", StringComparison.OrdinalIgnoreCase) || action.Action.Equals("challenge", StringComparison.OrdinalIgnoreCase)))
                ThrowIfNotAuthenticated();

            await base.SendActionAsync(action, responseHandler, cancellationToken);
        }

        protected void ThrowIfNotAuthenticated()
        {
            if (!IsAuthenticated)
            {
                string msg = $"the connection is not authenticated, user: {_parameters.Username}, encrypted: {_parameters.UseMD5Authentication}";
                throw new InvalidOperationException(msg);
            }
        }

        #endregion
        #region MISC

        /// <summary>
        ///     Notifying all handlers of the disconnection
        /// </summary>
        /// <param name="args"></param>
        protected override void OnDisconnectedTrigger(DisconnectEventArgs args)
        {
            FailAllHandlers(new NotConnectedException($"Connection lost: {args.Cause}, permanent: {args.IsPermanent}"));
            base.OnDisconnectedTrigger(args);
        }

        #endregion
        #region MISC - NOT TESTED


        public void RegisterUserEventClass(Type userEventClass)
        {
            if (userEventClass == null) throw new ArgumentNullException(nameof(userEventClass));
            if (!typeof(ManagerEvent).IsAssignableFrom(userEventClass) && !typeof(IManagerEvent).IsAssignableFrom(userEventClass))
                throw new ArgumentException("Type must derive from ManagerEvent or implement IManagerEvent.", nameof(userEventClass));
            
            // Register in ManagerEventBuilder (global registration)
            ManagerEventBuilder.RegisterUserEventClass(userEventClass);
            
            // Also register in active events if it's different and supports registration
            if (_activeEvents != _internalEvents)
            {
                _activeEvents.RegisterUserEventClass(userEventClass);
            }
        }

        public void Use(IManagerEventSubscriptions events, bool disposable = false)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            
            // If the new event manager is the same instance, no need to replace
            if (ReferenceEquals(_activeEvents, events))
            {
                _logger.LogDebug("Event manager is already the same instance, no replacement needed");
                return;
            }

            var previousEvents = _activeEvents;
            var wasUsingExternal = _externalEvents != null;
            
            // Always switch to the new events system
            _activeEvents = events;
            _externalEvents = events;
            
            _logger.LogDebug("Event manager replaced - now using external events system");
            
            // Only dispose the previous external events if:
            // 1. disposable is true
            // 2. We were using external events (not internal)
            // 3. The previous events is different from the new one
            // 4. The previous events is not the internal events (never dispose internal via this method)
            if (disposable && wasUsingExternal && 
                previousEvents != null && 
                !ReferenceEquals(previousEvents, events) &&
                !ReferenceEquals(previousEvents, _internalEvents))
            {
                try
                {
                    previousEvents.Dispose();
                    _logger.LogDebug("Previous external event manager disposed after replacement");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing previous external event manager during replacement");
                }
            }
        }

        /// <summary>
        /// Switches back to using the internal event manager.
        /// Optionally disposes the current external event manager.
        /// </summary>
        /// <param name="disposeExternal">Whether to dispose the current external event manager</param>
        public void UseInternal(bool disposeExternal = false)
        {
            if (_activeEvents == _internalEvents)
            {
                _logger.LogDebug("Already using internal event manager");
                return;
            }

            var externalToDispose = disposeExternal ? _externalEvents : null;
            
            // Switch back to internal
            _activeEvents = _internalEvents;
            _externalEvents = null;
            
            _logger.LogDebug("Switched back to internal event manager");

            // Dispose external if requested
            if (externalToDispose != null)
            {
                try
                {
                    externalToDispose.Dispose();
                    _logger.LogDebug("External event manager disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing external event manager");
                }
            }
        }

        #endregion
        #region DISPOSE

        public bool IsDisposeRequested { get; internal set; }

        public event EventHandler? OnDisposing;

        /// <summary>
        ///     Overrides the base Dispose method to orchestrate a graceful shutdown of all components.
        /// </summary>
        public override void Dispose()
        {
            // Check if Dispose has already been called to avoid redundant cleanup.
            if (IsDisposed || IsDisposeRequested)
                return;

            IsDisposeRequested = true;

            OnDisposing?.Invoke(this, EventArgs.Empty);

            // The order of disposal is crucial for a clean shutdown.
            // 1. Stop the reconnector to prevent it from trying to revive a connection being deliberately closed.
            _reconnector.Dispose();

            // 2. Stop the liveness monitor to cease sending pings.
            _livenessMonitor.Dispose();

            // 3. Dispose of the authenticator to unsubscribe from connection events.
            _authenticator.Dispose();

            // Sinalize que não haverá mais itens
            _packetChannel.Writer.Complete();

            // Opcional mas recomendado: aguardar a tarefa do consumidor terminar de processar
            // o que já estava na fila. Adicione um timeout para não bloquear para sempre.
            _packetConsumerTask.Wait(TimeSpan.FromSeconds(2));

            // 4. Always dispose the internal events system (we own it)
            try
            {
                _internalEvents?.Dispose();
                _logger.LogDebug("Internal event manager disposed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing internal event manager");
            }

            // 5. Never dispose external events system (we don't own it)
            if (_externalEvents != null)
            {
                _logger.LogDebug("External event manager not disposed (not owned by connection)");
            }

            // 6. Call the base Dispose method. This will trigger the cleanup in ActionDispatcher 
            //    (failing pending handlers) and then AMISocketManager (closing the socket).
            base.Dispose();

            // Inform the Garbage Collector that this object has been cleaned up and its finalizer doesn't need to run.
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}