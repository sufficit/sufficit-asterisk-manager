using AsterNET.Manager.Action;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Connection;
using Sufficit.Asterisk.Manager.Response;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager
{
    public static partial class ManagerConnectionExtensions
    {
        private static readonly ILogger _logger = ManagerLogger.CreateLogger(typeof(ManagerConnectionExtensions));

        public static async Task Refresh (this IManagerConnection source, CancellationToken cancellationToken)
        {
            // Cast to ManagerConnection to access SendActionAsync method
            if (source is not ManagerConnection connection)
                throw new InvalidOperationException("Connection must be a ManagerConnection instance");
                
            {
                var action = new SIPPeersAction();
                var response = await connection.SendActionAsync(action, cancellationToken);
                if (!response.IsSuccess())
                    throw new Exception($"error at executing {nameof(SIPPeersAction).ToLower()}");
            }
            {
                var action = new StatusAction();
                var response = await connection.SendActionAsync(action, cancellationToken); 
                if (!response.IsSuccess())
                    throw new Exception($"error at executing {nameof(StatusAction).ToLower()}");
            }            
        }

        public static async Task GetQueueStatus (this IManagerConnection source, string queue, string member, CancellationToken cancellationToken)
        {
            // Cast to ManagerConnection to access SendActionAsync method
            if (source is not ManagerConnection connection)
                throw new InvalidOperationException("Connection must be a ManagerConnection instance");
                
            var action = new QueueStatusAction
            {
                Queue = queue,
                Member = member
            };

            var response = await connection.SendActionAsync(action, cancellationToken);
            if (!response.IsSuccess())
                throw new Exception($"error at executing {nameof(QueueStatusAction).ToLower()}");
        }
    }
}
