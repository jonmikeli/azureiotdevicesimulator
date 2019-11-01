using Newtonsoft.Json;

namespace IoT.Simulator.Settings
{
    public class DeviceSettings : SettingsBase
    {
        public string ArtifactId
        {
            get { return base.DeviceId; }
        }

        [JsonProperty("simulationSettings")]
        public SimulationSettingsDevice SimulationSettings
        { get; set; }
    }
}
