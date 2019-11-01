using Newtonsoft.Json;
using System.Collections.Generic;

namespace IoT.Simulator.Models
{
    public class MeasuredData
    {
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("moduleId")]
        public string ModuleId { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("schemaVersion")]
        public string SchemaVersion { get; set; }

        [JsonProperty("messageType")]
        public string MessageType { get; set; }

        [JsonProperty("data")]
        public IEnumerable<DataItem> Data { get; set; }

    }
}
