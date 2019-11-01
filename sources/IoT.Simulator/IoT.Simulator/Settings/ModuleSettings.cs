using IoT.Simulator.Extensions;
using Newtonsoft.Json;

namespace IoT.Simulator.Settings
{
    public class ModuleSettings : SettingsBase
    {
        public string ModuleId
        {
            get
            {
                if (!string.IsNullOrEmpty(ConnectionString))
                    return ConnectionString.ExtractValue("ModuleId");
                else
                    return string.Empty;
            }
        }

        public string ArtifactId
        {
            get
            {
                if (!string.IsNullOrEmpty(ConnectionString))
                    return $@"{DeviceId}/{ModuleId}";
                else
                    return string.Empty;
            }
        }

        [JsonProperty("simulationSettings")]
        public SimulationSettingsModule SimulationSettings
        {
            get; set;
        }
    }
}
