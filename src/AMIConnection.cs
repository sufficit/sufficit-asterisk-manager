﻿using AsterNET.Manager;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sufficit.Asterisk.Manager
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

        public AMIConnection(ILogger<ManagerConnection> logger, AMIProviderOptions options) : base(logger, options.Address, options.Port, options.User, options.Password)
        {
            Title = options.Title;
        }
    }
}
