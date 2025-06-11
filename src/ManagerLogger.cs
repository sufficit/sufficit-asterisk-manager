using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sufficit.Asterisk.Manager
{
    public static class ManagerLogger
    {
        public static ILoggerFactory LoggerFactory { get; set; } = new LoggerFactory();

        public static ILogger CreateLogger<T>()
            => LoggerFactory.CreateLogger<T>();

        public static ILogger CreateLogger(Type type)
            => LoggerFactory.CreateLogger(type);

        public static ILogger CreateLogger(string categoryName)
            => LoggerFactory.CreateLogger(categoryName);
    }
}
