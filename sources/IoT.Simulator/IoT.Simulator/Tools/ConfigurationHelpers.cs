using IoT.Simulator.Exceptions;
using System;
using System.IO;
using System.Text;

namespace IoT.Simulator.Tools
{
    public static class ConfigurationHelpers
    {
        public static void CheckEnvironmentConfigurationFiles(string environment)
        {
            if (string.IsNullOrEmpty(environment))
                throw new ArgumentNullException(nameof(environment));

            StringBuilder sb = new StringBuilder();

            var appsettings = $"appsettings.{environment}.json";
            if (!File.Exists(appsettings))
                sb.AppendLine($"{appsettings} not found.");

            var devicesettings = $"devicesettings.{environment}.json";
            if (!File.Exists(devicesettings))
                sb.AppendLine($"{devicesettings} not found.");

            var modulessettings = $"modulessettings.{environment}.json";
            if (!File.Exists(modulessettings))
                sb.AppendLine($"{modulessettings} not found.");

            if (sb.Length > 0)
                throw new MissingEnvironmentConfigurationFileException(sb.ToString());

        }
    }
}
