using AsterNET.Manager.Action;
using System.Threading.Tasks;
using System.Threading;
using Sufficit.Asterisk.Manager.Response;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Connection;

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
