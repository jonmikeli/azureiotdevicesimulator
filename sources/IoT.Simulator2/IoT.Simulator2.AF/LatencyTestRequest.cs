using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IoT.Simulator2.Latency
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
