using Newtonsoft.Json;

namespace IoT.Simulator.Settings
{
    public class SimulationSettingsDevice : SimulationSettingsBase
    {

        [JsonProperty("enableLatencyTests")]
        public bool EnableLatencyTests { get; set; }
        [JsonProperty("latencyTestsFrecuency")]
        public int LatencyTestsFrecuency { get; set; }

        [JsonProperty("enableDevice")]
        public bool EnableDevice { get; set; }
        [JsonProperty("enableModules")]
        public bool EnableModules { get; set; }


        [JsonProperty("enableFileUpload")]
        public bool EnableFileUpload { get; set; }
        [JsonProperty("numberOfMessagesToUpload")]
        public int NumberOfMessagesToUpload { get; set; }
        [JsonProperty("fileUploadStorage")]
        public BlobStorageSettings FileUploadStorage { get; set; }
    }
}
