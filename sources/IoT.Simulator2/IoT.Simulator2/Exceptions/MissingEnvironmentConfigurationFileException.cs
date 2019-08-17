using System;
using System.Collections.Generic;
using System.Text;

namespace IoT.Simulator2.Exceptions
{
    public class MissingEnvironmentConfigurationFileException : ArgumentException
    {
        public MissingEnvironmentConfigurationFileException(string message) : base(message) { }
    }
}
