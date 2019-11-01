using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace IoT.Simulator.Latency
{
    public class LatencyTestRequest
    {
        [JsonProperty("deviceId")]
        [Required]
        public string DeviceId { get; set; }

        [JsonProperty("startTimestamp")]
        [Required]
        public int StartTimestamp { get; set; }
    }
}
