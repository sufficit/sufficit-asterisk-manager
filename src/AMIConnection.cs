using Sufficit.Asterisk.Manager.Configuration;
using Sufficit.Asterisk.Manager.Connection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sufficit.Asterisk.Manager
{
    /// <summary>
    /// Extends the standard ManagerConnection, serving solely to include a title for the server. <br />
    /// This facilitates event tracking and visualization.
    /// </summary>
    public class AMIConnection : ManagerConnection
    {
        /// <summary>
        ///     Title for the provider.
        /// </summary>
        public string Title { get; }

        public AMIConnection (AMIProviderOptions options) : base(
            new ManagerConnectionParameters()
            {
                KeepAlive = options.KeepAlive,
                Hostname = options.Address,
                Port = options.Port,
                Username = options.User ?? string.Empty,
                Password = options.Password ?? string.Empty,
            })
        {
            Title = options.Title;
        }
    }
}
