using System;

namespace IoT.Simulator2.Extensions
{
    public static class LoggerExtensions
    {
        public static string BuildLogPrefix(this string logType)
        {
            if (string.IsNullOrEmpty(logType))
                return null;

            return $"{DateTime.Now}::logType:{logType}";
        }
    }
}
