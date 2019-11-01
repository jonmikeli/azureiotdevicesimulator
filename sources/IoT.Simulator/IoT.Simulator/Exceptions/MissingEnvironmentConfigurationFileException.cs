using System;

namespace IoT.Simulator.Exceptions
{
    public class MissingEnvironmentConfigurationFileException : ArgumentException
    {
        public MissingEnvironmentConfigurationFileException(string message) : base(message) { }
    }
}
