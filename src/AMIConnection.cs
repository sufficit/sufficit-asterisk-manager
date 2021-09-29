using AsterNET.Manager;
using Sufficit.AsteriskManager.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sufficit.AsteriskManager
{
    /// <summary>
    /// Extende do ManagerConnection normal, serve unicamente para incluir um titulo para o servidor <br />
    /// Facilita no rastreamento e visualização dos eventos
    /// </summary>
    public class AMIConnection : ManagerConnection
    {        
        /// <summary>
        /// Titulo do provedor
        /// </summary>
        public string Title { get; }

        public AMIConnection(AMIProviderOptions options) : base(options.Address, options.Port, options.User, options.Password)
        {
            Title = options.Title;
        }
    }
}
