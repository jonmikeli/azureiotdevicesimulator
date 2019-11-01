using Newtonsoft.Json;

namespace IoT.Simulator.Settings
{
    public class BlobStorageSettings
    {
        [JsonProperty("storageAccount")]
        public string StorageAccount { get; set; }
        [JsonProperty("storageContainer")]
        public string StorageContainer { get; set; }

        [JsonProperty("connectionString")]
        public string ConnectionString { get; set; }
    }
}
