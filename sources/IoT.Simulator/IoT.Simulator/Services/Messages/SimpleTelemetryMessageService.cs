using IoT.Simulator.Extensions;
using IoT.Simulator.Tools;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IoT.Simulator.Services
{
    //https://dejanstojanovic.net/aspnet/2018/december/registering-multiple-implementations-of-the-same-interface-in-aspnet-core/
    public class SimpleTelemetryMessageService : ITelemetryMessageService
    {
        private ILogger _logger;
        private string fileTemplatePath = @"./Messages/measureddata.json";

        public SimpleTelemetryMessageService(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<SimpleTelemetryMessageService>();
        }

        public async Task<string> GetMessageAsync()
        {
            string messageString = File.ReadAllText(fileTemplatePath);

            if (string.IsNullOrEmpty(messageString))
                throw new ArgumentNullException(nameof(messageString), "DATA: The message to send is empty or not found.");

            return messageString;
        }

        public async Task<string> GetMessageAsync(string deviceId, string moduleId)
        {
            string artifactId = string.IsNullOrEmpty(moduleId) ? deviceId : moduleId;

            string logPrefix = "SimpleTelemetryMessageService".BuildLogPrefix();
            string messageString = await GetMessageAsync();

            if (string.IsNullOrEmpty(messageString))
                throw new ArgumentNullException(nameof(messageString), "DATA: The message to send is empty or not found.");

            _logger.LogTrace($"{logPrefix}::{artifactId}::measureddata.json file loaded.");

            messageString = IoTTools.UpdateIds(messageString, deviceId, moduleId);
            _logger.LogTrace($"{logPrefix}::{artifactId}::DeviceId and moduleId updated in the message template.");

            return messageString;
        }

        public async Task<string> GetRandomizedMessageAsync(string deviceId, string moduleId)
        {
            string artifactId = string.IsNullOrEmpty(moduleId) ? deviceId : moduleId;

            string messageString = await this.GetMessageAsync(deviceId, moduleId);
            string logPrefix = "SimpleTelemetryMessageService".BuildLogPrefix();

            //Randomize data           
            messageString = IoTTools.RandomizeData(messageString);
            _logger.LogTrace($"{logPrefix}::{artifactId}::Randomized data to update template's values before sending the message.");

            return messageString;
        }
    }
}
