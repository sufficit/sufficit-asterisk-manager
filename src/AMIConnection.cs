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

        public AMIConnection (AMIProviderOptions options) : base(options)
        {
            Title = options.Title;
        }
    }
}
