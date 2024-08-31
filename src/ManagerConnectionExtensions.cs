using AsterNET.Manager.Action;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using AsterNET.Manager;

namespace Sufficit.Asterisk.Manager
{
    public static class ManagerConnectionExtensions
    {
        public static async Task Refresh(this ManagerConnection source, CancellationToken cancellationToken)
        {            
            {
                var action = new SIPPeersAction();
                var response = await source.SendActionAsync(action, cancellationToken);
                if (!response.IsSuccess())
                    throw new Exception($"error at executing {nameof(SIPPeersAction).ToLower()}");
            }
            {
                var action = new StatusAction();
                var response = await source.SendActionAsync(action, cancellationToken); 
                if (!response.IsSuccess())
                    throw new Exception($"error at executing {nameof(StatusAction).ToLower()}");
            }            
        }

        public static async Task GetQueueStatus(this ManagerConnection source, string queue, string member, CancellationToken cancellationToken)
        {
            var action = new QueueStatusAction
            {
                Queue = queue,
                Member = member
            };

            var response = await source.SendActionAsync(action, cancellationToken);
            if (!response.IsSuccess())
                throw new Exception($"error at executing {nameof(QueueStatusAction).ToLower()}");
        }
    }
}
