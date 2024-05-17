using AsterNET.Manager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk.Manager.Configuration;
using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager
{
    public class AsteriskManagerProvider : IAMIProvider, IDisposable
    {
        /// <summary>
        ///     Individual options for this provider
        /// </summary>
        public AMIProviderOptions Options { get; internal set; }

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
                SwitchConnection(_enabled);
            }
        }

        #endregion

        /// <summary>
        /// Muito inseguro, sendo usado temporáriamente <br />
        /// Somente usado para exibir o status na página web Sufficit.Web.Telefonia.Asterisk.Manager
        /// </summary>
        [JsonIgnore]
        public ManagerConnection? Connection => _connection;

        public ValueTask<ManagerConnection> GetValidConnection(CancellationToken cancellationToken)
            => Connect(false, cancellationToken);        

        private async void SwitchConnection(bool on)
        {
            bool connected = false;
            lock (_lockSwitchConnection) connected = _connection?.IsConnected() ?? false;

            if (on && !connected) await Connect(Options.KeepAlive);
            else if (!on && connected) await Disconnect();
        }

        private bool _enabled;

        /// <summary>
        /// Titulo do provedor, usado para prefixar logs
        /// </summary>
        public string Title => Options.Title;

        /// <summary>
        /// Conexão com o Asterisk
        /// </summary>
        private ManagerConnection? _connection;

        /// <summary>
        /// Sistema de logs padrão
        /// </summary>
        private readonly ILogger _logger;
        private readonly ILogger<ManagerConnection> _logmanager;

        /// <summary>
        /// Lock for connection on thread safe
        /// </summary>
        private readonly object _lockSwitchConnection;

        #region CONSTRUTORES

        public AsteriskManagerProvider(IOptions<AMIProviderOptions> options, ILogger<AsteriskManagerProvider> logger, ILogger<ManagerConnection> logManager)
        {
            _lockSwitchConnection = new object();
            _logger = logger;
            Options = options.Value;

            _logmanager = logManager;
        }

        #endregion
        #region FUNÇÕES BASICAS DA CONEXAO

        /// <summary>
        ///     Realiza a conexão com o servidor caso ela ainda não esteja aberta
        /// </summary>
        public async ValueTask<ManagerConnection> Connect(bool KeepAlive = true, CancellationToken cancellationToken = default)
        {
            if (_connection == null || _connection.IsDisposed)
            {
                _connection = new AMIConnection(_logmanager, Options);
                _connection.Events.FireAllEvents = false;
                _connection.Events.Async = true;
            }

            if (!_connection.IsConnected())
            {               
                _connection.KeepAlive = KeepAlive;
                _connection.KeepAliveAfterAuthenticationFailure = false;
                _connection.ReconnectRetryMax = int.MaxValue;
                _connection.ReconnectIntervalMax = 30000; // 30 segundos
                _connection.DefaultEventTimeout = _connection.DefaultResponseTimeout = 10000;
                
                // lock (_lockSwitchConnection)
                await _connection.Login(cancellationToken);

                _logger.LogInformation("MANAGER: {text}; ASTERISK: {enum}", _connection.Version, _connection.AsteriskVersion);
            }

            return _connection;
        }

        public async Task Disconnect(CancellationToken cancellationToken = default)
        {
            if (_connection?.IsConnected() ?? false)
            {
                await _connection.LogOff(cancellationToken);               
            }
        }

        #endregion
        #region DISPOSING

        /// <summary>
        ///     Invoked when none of these resources are necessary anymore
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // se não for nula a conexão
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
        }

        #endregion
    }
}
