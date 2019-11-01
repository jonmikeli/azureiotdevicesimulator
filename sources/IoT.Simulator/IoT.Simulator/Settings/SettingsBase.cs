using IoT.Simulator.Extensions;
using Newtonsoft.Json;

namespace IoT.Simulator.Settings
{
    public class SettingsBase
    {
        [JsonProperty("connectionString")]
        public string ConnectionString { get; set; }

        public string DeviceId
        {
            get
            {
                if (!string.IsNullOrEmpty(ConnectionString))
                    return ConnectionString.ExtractValue("DeviceId");
                else
                    return string.Empty;
            }
        }

        public string HostName
        {
            get
            {
                if (!string.IsNullOrEmpty(ConnectionString))
                    return ConnectionString.ExtractValue("HostName");
                else
                    return string.Empty;
            }
        }
    }
}
