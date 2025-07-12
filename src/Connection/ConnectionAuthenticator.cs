using AsterNET.Manager.Action;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.IO;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using Sufficit.Asterisk.Manager.Response;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager.Connection
{
    /// <summary>
    /// Handles the authentication process (login and logoff) with the Asterisk server.
    /// It listens to the lifecycle manager's events to know when to start authentication.
    /// </summary>
    public class ConnectionAuthenticator : IDisposable
    {
        private static readonly ILogger _logger = ManagerLogger.CreateLogger<ConnectionAuthenticator>();

        private readonly ManagerConnectionParameters _parameters;
        private readonly IAMISocketManager _lifecycleManager;
        private readonly IActionDispatcher _actionDispatcher;
        private TaskCompletionSource<bool>? _loginTcs;

        public bool IsAuthenticated { get; private set; }
        public Func<ValueTask>? OnAuthenticated { get; set; }

        public ConnectionAuthenticator(ManagerConnectionParameters parameters, IAMISocketManager lifecycleManager, IActionDispatcher actionDispatcher)
        {
            _parameters = parameters;
            _lifecycleManager = lifecycleManager;
            _actionDispatcher = actionDispatcher;

            // Subscribe to lifecycle events
            _lifecycleManager.OnConnectionIdentified += OnConnectionIdentified;
            _lifecycleManager.OnDisconnected += OnDisconnected;
        }

        public async Task Login (CancellationToken cancellationToken)
        {
            if (IsAuthenticated) return;

            bool authSuccess = false;

            _loginTcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => _loginTcs.TrySetCanceled()))
            {
                if (!_lifecycleManager.IsConnected)
                {
                    if (!await _lifecycleManager.Connect(cancellationToken))
                    {
                        throw new NotConnectedException("Failed to establish physical connection.");
                    }
                }

                // The OnConnectionIdentified event will trigger the rest of the login process
                authSuccess = await _loginTcs.Task;
            }

            if (authSuccess && OnAuthenticated != null) 
                await OnAuthenticated.Invoke();
        }

        private async void OnConnectionIdentified(object? sender, string protocolIdentifier)
        {
            _logger.LogTrace("Protocol Identifier '{ProtocolId}' received. Proceeding with authentication.", protocolIdentifier);
            try
            {
                await PerformAuthenticationAsync();                
                IsAuthenticated = true;

                _logger.LogDebug("Authentication successful for user '{LoginUsername}'.", _parameters.Username);
                _loginTcs?.TrySetResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication failed.");
                _lifecycleManager.Disconnect("Authentication failed", isPermanent: false);
                _loginTcs?.TrySetException(ex);
            }
        }

        private async Task PerformAuthenticationAsync()
        {
            var action = new LoginAction(_parameters.Username, "plaintext", _parameters.Password, "on");

            if (_parameters.UseMD5Authentication)
            {
                var challengeAction = new ChallengeAction("MD5");
                var challengeResponse = await _actionDispatcher.SendActionAsync<ChallengeResponse>(challengeAction, CancellationToken.None);

                var challengeBytes = System.Text.Encoding.UTF8.GetBytes(challengeResponse.Challenge + _parameters.Password);
#if NETSTANDARD
                var md5 = System.Security.Cryptography.MD5.Create();
                var hash = md5.ComputeHash(challengeBytes);
#else
                var hash = System.Security.Cryptography.MD5.HashData(challengeBytes);                
#endif
                action.Key = BitConverter.ToString(hash).Replace("-", "").ToLower();
                action.AuthType = challengeAction.AuthType;
            }

            var response = await _actionDispatcher.SendActionAsync<ManagerResponseEvent>(action, CancellationToken.None);
            if (response.Exception != null)
                throw new AuthenticationFailedException($"authentication failed for user: {action.Username}", response.Exception);
        }

        /* OLD METHOD
         
        private async Task PerformAuthenticationAsync(CancellationToken cancellationToken)
        {
            var action = new LoginAction(_parameters.Username, "plaintext", _parameters.Password, "on");
            if (_parameters.UseMD5Authentication)
            {
                action.AuthType = "MD5";
                var challengeAction = new ChallengeAction(action.AuthType);
                var challengeResp = await SendActionAsync<ChallengeResponse>(challengeAction, cancellationToken);
                if (string.IsNullOrEmpty(challengeResp.Challenge))
                    throw new AuthenticationFailedException("challenge for MD5 login was null or empty.");

                var md = AsterNET.Util.MD5Support.GetInstance();
                md.Update(_parameters.SocketEncoding.GetBytes(challengeResp.Challenge));
                md.Update(_parameters.SocketEncoding.GetBytes(action.Key ?? ""));
                action.Key = Helper.ToHexString(md.DigestData);
            }

            _logger.LogWarning("sending a ({type}) login action for user: {user}", action.AuthType, action.Username);
            var response = await SendActionAsync(action, cancellationToken);
            if (response.Exception != null)
                throw new AuthenticationFailedException($"authentication failed for user: {action.Username}", response.Exception);

            _logger.LogInformation("Authentication successful for user '{LoginUsername}'", _parameters.Username);
            IsAuthenticated = true;
        }
          
        */

        private void OnDisconnected(object? sender, EventArgs e)
        {
            IsAuthenticated = false;
        }

        public async Task LogOff(CancellationToken cancellationToken)
        {
            if (!IsAuthenticated) return;
            await _actionDispatcher.SendActionAsync<ManagerResponseEvent>(new LogoffAction(), cancellationToken);
            _lifecycleManager.Disconnect("User requested logoff", isPermanent: true);
        }

        public void Dispose()
        {
            _lifecycleManager.OnConnectionIdentified -= OnConnectionIdentified;
            _lifecycleManager.OnDisconnected -= OnDisconnected;
        }
    }
}
