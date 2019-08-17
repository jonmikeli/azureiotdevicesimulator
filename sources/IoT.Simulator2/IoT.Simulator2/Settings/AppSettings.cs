using Microsoft.Extensions.Logging;

namespace IoT.Simulator2.Settings
{
    public class AppSettings
    {
        public Logging Logging { get; set; }
    }

    public class Logging
    {
        public LogLevel Default { get; set; }
    }

}
