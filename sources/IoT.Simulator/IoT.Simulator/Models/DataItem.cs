using Newtonsoft.Json;

namespace IoT.Simulator.Models
{
    public class DataItem
    {
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("propertyName")]
        public string PropertyName { get; set; }

        [JsonProperty("propertyValue")]
        public string PropertyValue { get; set; }

        [JsonProperty("propertyUnit")]
        public string PropertyUnit { get; set; }

        [JsonProperty("propertyDivFactor")]
        public int PropertyDivFactor { get; set; }
    }
}
