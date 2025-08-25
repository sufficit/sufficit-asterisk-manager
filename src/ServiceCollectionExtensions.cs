using AsterNET;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sufficit.Asterisk.Manager
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSufficitAsteriskManager(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider(false);
            var factory = provider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
            return services.AddSufficitAsteriskManager(factory);
        }

        /// <summary>
        ///     For now, only sets a log factory for the Asterisk Manager Classes.
        /// </summary>
        public static IServiceCollection AddSufficitAsteriskManager(this IServiceCollection services, ILoggerFactory? factory = null)
        {
            if (factory is not null)
            {
                ManagerLogger.LoggerFactory = factory;
                AsteriskLogger.LoggerFactory = factory;
            }

            return services;
        }
    }
}
