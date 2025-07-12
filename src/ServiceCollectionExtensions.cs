using AsterNET;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sufficit.Asterisk.Manager
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     For now, only sets a log factory for the Asterisk Manager Classes.
        /// </summary>
        public static IServiceCollection AddSufficitAsteriskManager(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var factory = provider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
            if (factory is not null)
            {
                ManagerLogger.LoggerFactory = factory;
                AsteriskLogger.LoggerFactory = factory;
            }

            return services;
        }
    }
}
