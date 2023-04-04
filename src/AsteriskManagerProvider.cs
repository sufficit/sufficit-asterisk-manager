using AsterNET.Manager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk.Manager.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager
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

        /// <summary>
        /// Lock for connection on thread safe
        /// </summary>
        private readonly object _lockSwitchConnection;

        #region CONSTRUTORES

        public AsteriskManagerProvider(IOptions<AMIProviderOptions> options, ILogger<AsteriskManagerProvider> logger, ILogger<ManagerConnection> logManager)
        {
            _lockSwitchConnection = new object();
            _logger = logger;
            _options = options.Value;

            _connection = new AMIConnection(logManager, _options);
            _connection.FireAllEvents = false;
            _connection.UseASyncEvents = true;
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
                _logger.LogInformation("MANAGER: " + _connection.Version + " ; ASTERISK: " + _connection.AsteriskVersion);
            }
        }

        public async Task Disconnect(CancellationToken cancellationToken = default)
        {
            if (_connection.IsConnected())
            {
                await _connection.LogOff(cancellationToken);               
            }
        }

        /// <summary>
        /// Tenta desfazer a conexão caso não esteja programado para usar o keepalive
        /// </summary>
        public async void Dispose()
        {
            if (!_options.KeepAlive) // se não for para manter aberto
            {
                if (_connection != null) // se não for nula a conexão
                {
                    if (_connection.IsConnected()) // se ainda estiver connectado
                    {
                        try { await Disconnect(); } catch { }
                    }
                }
            }
        }        

        #endregion
    }
}
