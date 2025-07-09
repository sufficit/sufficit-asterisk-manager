using AsterNET.Manager.Action;
using System;
using System.Threading.Tasks;
using System.Threading;
using Sufficit.Asterisk.Manager.Response;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Connection;

namespace Sufficit.Asterisk.Manager
{
    public static partial class ManagerConnectionExtensions
    {
        public static async Task SIPShowRegistry (this IManagerConnection source, CancellationToken cancellationToken)
        {
            // Cast to ManagerConnection to access SendActionAsync method
            if (source is not ManagerConnection connection)
                throw new InvalidOperationException("Connection must be a ManagerConnection instance");
                
            var action = new SIPShowRegistryAction();
            var response = await connection.SendActionAsync(action, cancellationToken);
            response.ThrowIfNotSuccess(action);
        }

        /// <summary>
        ///     CLI Command (sip reload)
        /// </summary>
        public static async Task SIPReload (this IManagerConnection source, CancellationToken cancellationToken)
        {
            // Cast to ManagerConnection to access SendActionAsync method
            if (source is not ManagerConnection connection)
                throw new InvalidOperationException("Connection must be a ManagerConnection instance");
                
            var action = new CommandAction("sip reload");
            var response = await connection.SendActionAsync(action, cancellationToken);                        
            response.ThrowIfNotSuccess(action);
        }        
    }
}
