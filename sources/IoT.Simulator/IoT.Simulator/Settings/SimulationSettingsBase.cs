using IoT.Simulator.Tools;
using Newtonsoft.Json;
using System;

namespace IoT.Simulator.Settings
{
    public class SimulationSettingsBase
    {
        [JsonProperty("enableTelemetryMessages")]
        public bool EnableTelemetryMessages { get; set; }
        [JsonProperty("telemetryFrecuency")]
        public int TelemetryFrecuency { get; set; }
        [JsonProperty("telemetryLogColor")]
        public ConsoleColor TelemetryLogColor { get; set; }

        [JsonProperty("enableErrorMessages")]
        public bool EnableErrorMessages { get; set; }
        [JsonProperty("errorFrecuency")]
        public int ErrorFrecuency { get; set; }
        [JsonProperty("errorLogColor")]
        [JsonConverter(typeof(StringToConsoleColorConverter))]
        public ConsoleColor ErrorLogColor { get; set; }

        [JsonProperty("enableCommissioningMessages")]
        public bool EnableCommissioningMessages { get; set; }
        [JsonProperty("commissioningFrecuency")]
        public int CommissioningFrecuency { get; set; }
        [JsonProperty("commissioningLogColor")]
        public ConsoleColor CommissioningLogColor { get; set; }



        [JsonProperty("enableTwinReportedMessages")]
        public bool EnableTwinReportedMessages { get; set; }

        [JsonProperty("twinReportedMessagesFrecuency")]
        public int TwinReportedMessagesFrecuency { get; set; }
        [JsonProperty("twinReportedLogColor")]
        public ConsoleColor TwinReportedLogColor { get; set; }


        [JsonProperty("enableReadingTwinProperties")]
        public bool EnableReadingTwinProperties { get; set; }
        [JsonProperty("readingTwinLogColor")]
        public ConsoleColor ReadingTwinLogColor { get; set; }


        [JsonProperty("enableC2DDirectMethods")]
        public bool EnableC2DDirectMethods { get; set; }
        [JsonProperty("C2DDirectMethodsLogColor")]
        public ConsoleColor C2DDirectMethodsLogColor { get; set; }

        [JsonProperty("enableC2DMessages")]
        public bool EnableC2DMessages { get; set; }
        [JsonProperty("C2DMessagesLogColor")]
        public ConsoleColor C2DMessagesLogColor { get; set; }

        [JsonProperty("enableTwinPropertiesDesiredChangesNotifications")]
        public bool EnableTwinPropertiesDesiredChangesNotifications { get; set; }
        [JsonProperty("twinPropertiesDesiredChangesNotificationsLogColor")]
        public ConsoleColor TwinPropertiesDesiredChangesNotificationsLogColor { get; set; }
    }
}
