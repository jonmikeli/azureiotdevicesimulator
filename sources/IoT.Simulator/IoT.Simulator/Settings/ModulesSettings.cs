using Newtonsoft.Json;
using System.Collections.Generic;

namespace IoT.Simulator.Settings
{
    public class ModulesSettings
    {
        [JsonProperty("modules")]
        public IEnumerable<ModuleSettings> Modules { get; set; }
    }
}
