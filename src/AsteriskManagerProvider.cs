using AsterNET.Manager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk.Manager.Configuration;
using Sufficit.Asterisk.Manager.Connection;
using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager
{
    public class AsteriskManagerProvider : IAMIProvider, IDisposable
    {
        public AMIProviderOptions Options { get; internal set; }

        public string Title => Options.Title;

        public bool Enabled { get; private set; }

        [JsonIgnore]
        public ManagerConnection? Connection => _connection;

        private ManagerConnection? _connection;
        private readonly ILogger _logger;
        private readonly object _lockConnection = new object();

        private readonly SemaphoreSlim _stateChangeSemaphore = new SemaphoreSlim(1, 1);

        public AsteriskManagerProvider(IOptions<AMIProviderOptions> options, ILogger<AsteriskManagerProvider> logger)
        {
            _logger = logger;
            Options = options.Value;
        }

        public Task<ManagerConnection> ConnectAsync(CancellationToken cancellationToken = default)
            => ConnectAsync(null, cancellationToken);

        /// <summary>
        /// Asynchronously connects the provider to the Asterisk server and returns the valid connection object.
        /// </summary>
        public async Task<ManagerConnection> ConnectAsync(bool? keepalive = null, CancellationToken cancellationToken = default)
        {
            await _stateChangeSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (Enabled && _connection != null && _connection.IsConnected)
                {
                    _logger.LogInformation("Provider '{Title}' is already connected.", Options.Title);
                    return _connection;
                }

                _logger.LogInformation("Connecting Asterisk Manager Provider: {Title}", Options.Title);
                var connection = await InternalConnect(keepalive ?? Options.KeepAlive, cancellationToken);
                Enabled = true;
                _logger.LogInformation("Provider '{Title}' connected successfully.", Options.Title);

                return connection;
            }
            finally
            {
                _stateChangeSemaphore.Release();
            }
        }

        /// <summary>
        /// Asynchronously disconnects the provider from the Asterisk server.
        /// </summary>
        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            await _stateChangeSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (!Enabled)
                {
                    _logger.LogInformation("Provider '{Title}' is already disconnected.", Options.Title);
                    return;
                }

                _logger.LogInformation("Disconnecting Asterisk Manager Provider: {Title}", Options.Title);
                await InternalDisconnect(cancellationToken);
                Enabled = false;
                _logger.LogInformation("Provider '{Title}' disconnected successfully.", Options.Title);
            }
            finally
            {
                _stateChangeSemaphore.Release();
            }
        }

        public ValueTask<ManagerConnection> GetValidConnection(CancellationToken cancellationToken)
            => InternalConnect(false, cancellationToken);

        private async ValueTask<ManagerConnection> InternalConnect(bool keepAlive = true, CancellationToken cancellationToken = default)
        {
            lock (_lockConnection)
            {
                if (_connection == null || _connection.IsDisposed)
                {
                    Options.KeepAlive = keepAlive;
                    Options.ReconnectIntervalMax = 30000;

                    _connection = new AMIConnection(Options);
                    _connection.Events.FireAllEvents = false;
                }
            }

            if (!_connection.IsConnected)
            {                
                await _connection.Login(cancellationToken);

                // _logger.LogInformation("MANAGER: {text}; ASTERISK: {enum}", _connection.Version, _connection.AsteriskVersion);
            }

            return _connection;
        }

        private Task InternalDisconnect(CancellationToken cancellationToken = default)
        {
            if (_connection?.IsConnected ?? false)
            {
                return _connection.LogOff(cancellationToken);
            }
            return Task.CompletedTask;
        }

        #region DISPOSING
        public bool IsDisposed { get; internal set; }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;

                _stateChangeSemaphore.Wait(TimeSpan.FromSeconds(5));
                if (_connection != null)
                {
                    if (_connection.IsConnected)
                    {
                        try { InternalDisconnect().Wait(2000); }
                        catch { /* Ignoring errors on dispose */ }
                    }
                    _connection.Dispose();
                    _connection = null;
                }

                _stateChangeSemaphore.Release();
                _stateChangeSemaphore.Dispose();

                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }           
        }

        protected virtual void Dispose(bool disposing) { }

        #endregion
    }
}