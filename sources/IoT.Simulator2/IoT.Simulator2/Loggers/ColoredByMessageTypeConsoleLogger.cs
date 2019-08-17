using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace IoT.Simulator2.Loggers
{
    //https://www.tutorialdocs.com/article/aspnet-core-log-components.html
    public class ColoredByMessageTypeConsoleLogger : ILogger
    {
        private readonly string _service;
        private readonly ColoredByMessageTypeConsoleLoggerConfiguration _config;

        public ColoredByMessageTypeConsoleLogger(string service, ColoredByMessageTypeConsoleLoggerConfiguration config)
        {
            if (string.IsNullOrEmpty(service))
                throw new ArgumentNullException(nameof(service));

            if (config == null)
                throw new ArgumentNullException(nameof(ColoredByMessageTypeConsoleLoggerConfiguration));

            _service = service;
            _config = config;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return !string.IsNullOrEmpty(_config?.LogType);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            //TODO: look for better ways to be precise in terms of message filtering
            if (formatter != null && formatter(state, exception).Contains($"logType:{_config?.LogType}"))
            {
                object _object = new object();
                lock (_object)
                {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = _config.Color;
                    Console.WriteLine($"{logLevel.ToString()}-{_service}-{_config?.LogType}-{formatter(state, exception)}");
                    Console.ForegroundColor = color;
                }
            }
        }
    }    
}
