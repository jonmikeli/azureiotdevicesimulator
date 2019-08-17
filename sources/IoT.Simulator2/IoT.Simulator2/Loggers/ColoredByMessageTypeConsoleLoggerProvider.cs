using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace IoT.Simulator2.Loggers
{
    public class ColoredByMessageTypeConsoleLoggerProvider : ILoggerProvider
    {
        private readonly ColoredByMessageTypeConsoleLoggerConfiguration _config;

        public ColoredByMessageTypeConsoleLoggerProvider(ColoredByMessageTypeConsoleLoggerConfiguration config)
        {
            _config = config;
        }

        public ILogger CreateLogger(string service)
        {
            return new ColoredByMessageTypeConsoleLogger(service, _config);
        }

        public void Dispose()
        {

        }
    }
}
