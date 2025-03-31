using AsterNET.Manager.Action;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using AsterNET.Manager;
using Sufficit.Asterisk.Manager.Response;

namespace Sufficit.Asterisk.Manager
{
    public static partial class ManagerConnectionExtensions
    {
        public static async Task SIPShowRegistry (this ManagerConnection source, CancellationToken cancellationToken)
        {
            var action = new SIPShowRegistryAction();
            var response = await source.SendActionAsync(action, cancellationToken);
            response.ThrowIfNotSuccess(action);
        }

        /// <summary>
        ///     CLI Command (sip reload)
        /// </summary>
        public static async Task SIPReload (this ManagerConnection source, CancellationToken cancellationToken)
        {
            var action = new CommandAction("sip reload");
            var response = await source.SendActionAsync(action, cancellationToken);                        
            response.ThrowIfNotSuccess(action);
        }        
    }
}
