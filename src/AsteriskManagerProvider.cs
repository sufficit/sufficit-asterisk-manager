using AsterNET.Manager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.AsteriskManager.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.AsteriskManager
{
    public class AsteriskManagerProvider : IAMIProvider, IDisposable
    {
        #region IMPLEMENTAÇÃO DA INTEFACE IAMIProvider

        public bool Enabled 
        { 
            get 
            {
                return _enabled;
            }
            set
            {
                _enabled = value;

                // Aciona de forma assíncrona o processo de mudança de estado, caso necessário
                switchConnection(_enabled);
            }
        }

        #endregion

        public EventNamespace Events { get; }

        /// <summary>
        /// Muito inseguro, sendo usado temporáriamente
        /// </summary>
        public ManagerConnection Connection => _connection;

        private async void switchConnection(bool on)
        {
            bool connected = false;
            lock (_lockSwitchConnection) connected = _connection.IsConnected();

            if (on && !connected) await Connect(_options.KeepAlive);
            else if (!on && connected) await Disconnect();
        }

        private bool _enabled;

        /// <summary>
        /// Titulo do provedor, usado para prefixar logs
        /// </summary>
        public string Title => _options.Title;

        /// <summary>
        /// Conexão com o Asterisk
        /// </summary>
        private readonly ManagerConnection _connection;

        /// <summary>
        /// Sistema de logs padrão
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Configurações individuais desse provedor
        /// </summary>
        private readonly AMIProviderOptions _options;
        private readonly object _lockSwitchConnection = new object();

        #region CONSTRUTORES

        /// <summary>
        /// Usado pelo sistema de Dependency injection
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="config"></param>
        public AsteriskManagerProvider(IOptions<AMIProviderOptions> options, ILogger<AsteriskManagerProvider> logger) : this(options.Value, logger)
        {            
            _logger?.LogInformation("Asterisk Manager Provider by dependency injection");
        }

        public AsteriskManagerProvider(AMIProviderOptions options, ILogger logger = default)
        {
            _logger = logger;
            _options = options;

            _connection = new AMIConnection(_options);
            _connection.FireAllEvents = false;
            _connection.UseASyncEvents = true;

            // Expondo eventos
            Events = new EventNamespace(ref _connection);
        }

        #endregion
        #region FUNÇÕES BASICAS DA CONEXAO

        /// <summary>
        /// Realiza a conexão com o servidor caso ela ainda não esteja aberta
        /// </summary>
        /// <param name="KeepAlive"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Connect(bool KeepAlive = true, CancellationToken cancellationToken = default)
        {
            if (!_connection.IsConnected())
            {
                _connection.KeepAlive = KeepAlive;
                _connection.KeepAliveAfterAuthenticationFailure = false;
                _connection.ReconnectRetryMax = int.MaxValue;
                _connection.ReconnectIntervalMax = 30000; // 30 segundos
                _connection.DefaultEventTimeout = _connection.DefaultResponseTimeout = 10000;

                await Task.Run(() => { lock (_lockSwitchConnection) _connection.Login(); }, cancellationToken);
                _logger?.LogInformation("MANAGER: " + _connection.Version + " ; ASTERISK: " + _connection.AsteriskVersion);
            }
        }

        public async Task Disconnect(CancellationToken cancellationToken = default)
        {
            if (_connection.IsConnected())
            {
                await Task.Run(() => { lock (_lockSwitchConnection) _connection.Logoff(); }, cancellationToken);               
            }
        }

        /// <summary>
        /// Tenta desfazer a conexão caso não esteja programado para usar o keepalive
        /// </summary>
        public async void Dispose()
        {
            if (!_options.KeepAlive)
            {
                try { await Disconnect(); }
                catch { }
            }
        }

        #endregion
        #region EVENTS

        public sealed class EventNamespace
        {
            readonly ManagerConnection _connection;
            public EventNamespace(ref ManagerConnection connection)
            {
                _connection = connection;
            }

            #region SECURITY

            public event EventHandler<AsterNET.Manager.Event.InvalidPasswordEvent> InvalidPassword
            {
                add { _connection.InvalidPassword += value; }
                remove { _connection.InvalidPassword -= value; }
            }

            public event EventHandler<AsterNET.Manager.Event.InvalidAccountIDEvent> InvalidAccountID
            {
                add { _connection.InvalidAccountID += value; }
                remove { _connection.InvalidAccountID -= value; }
            }

            public event EventHandler<AsterNET.Manager.Event.SuccessfulAuthEvent> SuccessfulAuth
            {
                add { _connection.SuccessfulAuth += value; }
                remove { _connection.SuccessfulAuth -= value; }
            }

            #endregion
            #region QUEUE STATUS

            public event EventHandler<AsterNET.Manager.Event.QueueParamsEvent> QueueParams
            {
                add { _connection.QueueParams += value; }
                remove { _connection.QueueParams -= value; }
            }

            public event EventHandler<AsterNET.Manager.Event.QueueMemberEvent> QueueMember
            {
                add { _connection.QueueMember += value; }
                remove { _connection.QueueMember -= value; }
            }

            public event EventHandler<AsterNET.Manager.Event.QueueEntryEvent> QueueEntry
            {
                add { _connection.QueueEntry += value; }
                remove { _connection.QueueEntry -= value; }
            }

            public event EventHandler<AsterNET.Manager.Event.QueueStatusCompleteEvent> QueueStatusComplete
            {
                add { _connection.QueueStatusComplete += value; }
                remove { _connection.QueueStatusComplete -= value; }
            }

            #endregion
            #region QUEUE STATUS EVENTS

            public event EventHandler<AsterNET.Manager.Event.QueueCallerAbandonEvent> QueueCallerAbandon
            {
                add { _connection.QueueCallerAbandon += value; }
                remove { _connection.QueueCallerAbandon -= value; }
            }

            public event EventHandler<AsterNET.Manager.Event.QueueCallerJoinEvent> QueueCallerJoin
            {
                add { _connection.QueueCallerJoin += value; }
                remove { _connection.QueueCallerJoin -= value; }
            }

            public event EventHandler<AsterNET.Manager.Event.QueueCallerLeaveEvent> QueueCallerLeave
            {
                add { _connection.QueueCallerLeave += value; }
                remove { _connection.QueueCallerLeave -= value; }
            }

            public event EventHandler<AsterNET.Manager.Event.QueueMemberAddedEvent> QueueMemberAdded
            {
                add { _connection.QueueMemberAdded += value; }
                remove { _connection.QueueMemberAdded -= value; }
            }

            public event EventHandler<AsterNET.Manager.Event.QueueMemberPauseEvent> QueueMemberPause
            {
                add { _connection.QueueMemberPause += value; }
                remove { _connection.QueueMemberPause -= value; }
            }

            public event EventHandler<AsterNET.Manager.Event.QueueMemberPenaltyEvent> QueueMemberPenalty
            {
                add { _connection.QueueMemberPenalty += value; }
                remove { _connection.QueueMemberPenalty -= value; }
            }

            public event EventHandler<AsterNET.Manager.Event.QueueMemberRemovedEvent> QueueMemberRemoved
            {
                add { _connection.QueueMemberRemoved += value; }
                remove { _connection.QueueMemberRemoved -= value; }
            }

            public event EventHandler<AsterNET.Manager.Event.QueueMemberRinginuseEvent> QueueMemberRinginuse
            {
                add { _connection.QueueMemberRinginuse += value; }
                remove { _connection.QueueMemberRinginuse -= value; }
            }

            public event EventHandler<AsterNET.Manager.Event.QueueMemberStatusEvent> QueueMemberStatus
            {
                add { _connection.QueueMemberStatus += value; }
                remove { _connection.QueueMemberStatus -= value; }
            }

            #endregion

            public event EventHandler<AsterNET.Manager.Event.ConnectionStateEvent> ConnectionState
            {
                add { _connection.ConnectionState += value; }
                remove { _connection.ConnectionState -= value; }
            }

            public event EventHandler<AsterNET.Manager.Event.PeerStatusEvent> PeerStatus
            {
                add { _connection.PeerStatus += value; }
                remove { _connection.PeerStatus -= value; }
            }
            
            public event EventHandler<AsterNET.Manager.Event.ExtensionStatusEvent> ExtensionStatus
            {
                add { _connection.ExtensionStatus += value; }
                remove { _connection.ExtensionStatus -= value; }
            }            
        }

        #endregion
    }
}
