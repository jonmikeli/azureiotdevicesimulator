using IoT.Simulator.Extensions;
using IoT.Simulator.Settings;
using IoT.Simulator.Tools;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading.Tasks;

namespace IoT.Simulator.Services
{
    public class ModuleSimulationService : IModuleSimulationService
    {
        public string ServiceId { get { return ModuleSettings?.ArtifactId; } }
        public ModuleSettings ModuleSettings { get; private set; }
        public SimulationSettingsModule SimulationSettings { get; private set; }
        private ILogger _logger;
        private int _telemetryInterval;
        private bool _stopProcessing;
        private ModuleClient _moduleClient;

        private ITelemetryMessageService _telemetryMessagingService;
        private IErrorMessageService _errorMessagingService;
        private ICommissioningMessageService _commissioningMessagingService;


        public ModuleSimulationService(ModuleSettings settings, SimulationSettingsModule simulationSettings, ITelemetryMessageService telemetryMessagingService, IErrorMessageService errorMessagingService, ICommissioningMessageService commissioningMessagingService, ILoggerFactory loggerFactory)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (simulationSettings == null)
                throw new ArgumentNullException(nameof(simulationSettings));

            if (telemetryMessagingService == null)
                throw new ArgumentNullException(nameof(telemetryMessagingService));

            if (errorMessagingService == null)
                throw new ArgumentNullException(nameof(errorMessagingService));

            if (commissioningMessagingService == null)
                throw new ArgumentNullException(nameof(commissioningMessagingService));

            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            string logPrefix = "system".BuildLogPrefix();

            ModuleSettings = settings;
            SimulationSettings = simulationSettings;
            _logger = loggerFactory.CreateLogger<ModuleSimulationService>();

            _telemetryMessagingService = telemetryMessagingService;
            _errorMessagingService = errorMessagingService;
            _commissioningMessagingService = commissioningMessagingService;

            _telemetryInterval = 10;
            _stopProcessing = false;

            _moduleClient = ModuleClient.CreateFromConnectionString(ModuleSettings.ConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Logger created.");
            _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Module simulator created.");
        }

        ~ModuleSimulationService()
        {
            if (_moduleClient != null)
            {
                _moduleClient.CloseAsync();
                _moduleClient.Dispose();

                string logPrefix = "system".BuildLogPrefix();

                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Module simulator disposed.");
            }
        }

        public async Task InitiateSimulationAsync()
        {
            string logPrefix = "system".BuildLogPrefix();

            IoTTools.CheckModuleConnectionStringData(ModuleSettings.ConnectionString, _logger);

            // Connect to the IoT hub using the MQTT protocol

            _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Module client created.");

            if (SimulationSettings.EnableTwinPropertiesDesiredChangesNotifications)
            {
                await _moduleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChange, null);
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Twin Desired Properties update callback handler registered.");
            }

            //Configuration
            if (SimulationSettings.EnableC2DDirectMethods)
                //Register C2D Direct methods handlers            
                await RegisterC2DDirectMethodsHandlersAsync(_moduleClient, ModuleSettings, _logger);

            if (SimulationSettings.EnableC2DMessages)
                //Start receiving C2D messages

                ReceiveC2DMessagesAsync(_moduleClient, ModuleSettings, _logger);

            //Messages
            if (SimulationSettings.EnableTelemetryMessages)
                SendDeviceToCloudMessagesAsync(_moduleClient, ModuleSettings.DeviceId, ModuleSettings.ModuleId, _logger); //interval is a global variable changed by processes

            if (SimulationSettings.EnableErrorMessages)
                SendDeviceToCloudErrorAsync(_moduleClient, ModuleSettings.DeviceId, ModuleSettings.ModuleId, SimulationSettings.ErrorFrecuency, _logger);

            if (SimulationSettings.EnableCommissioningMessages)
                SendDeviceToCloudCommissioningAsync(_moduleClient, ModuleSettings.DeviceId, ModuleSettings.ModuleId, SimulationSettings.CommissioningFrecuency, _logger);

            if (SimulationSettings.EnableReadingTwinProperties)
            {
                //Twins
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::INITIALIZATION::Retrieving twin.");
                Twin twin = await _moduleClient.GetTwinAsync();

                if (twin != null)
                    _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::INITIALIZATION::Device twin: {JsonConvert.SerializeObject(twin, Formatting.Indented)}.");
                else
                    _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::INITIALIZATION::No device twin.");
            }

            _moduleClient.SetConnectionStatusChangesHandler(new ConnectionStatusChangesHandler(ConnectionStatusChanged));
        }

        private void ConnectionStatusChanged(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            string logPrefix = "c2dmessages".BuildLogPrefix();

            _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Connection status changed-New status:{status.ToString()}-Reason:{reason.ToString()}.");
        }


        #region D2C                

        // Async method to send simulated telemetry
        internal async Task SendDeviceToCloudMessagesAsync(ModuleClient moduleClient, string deviceId, string moduleId, ILogger logger)
        {
            // Initial telemetry values
            int counter = 1;
            string logPrefix = "data".BuildLogPrefix();
            string messageString = string.Empty;

            using (logger.BeginScope($"{logPrefix}::{ModuleSettings.ArtifactId}::MEASURED DATA"))
            {
                while (true)
                {
                    //Randomize data
                    messageString = await _telemetryMessagingService.GetRandomizedMessageAsync(deviceId, moduleId);

                    var message = new Message(Encoding.UTF8.GetBytes(messageString));
                    message.Properties.Add("messageType", "data");

                    // Add a custom application property to the message.
                    // An IoT hub can filter on these properties without access to the message body.
                    //message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");
                    message.ContentType = "application/json";
                    message.ContentEncoding = "utf-8";

                    // Send the tlemetry message
                    await moduleClient.SendEventAsync(message);
                    counter++;

                    logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Sent message: {messageString}.");
                    logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::COUNTER: {counter}.");

                    if (_stopProcessing)
                    {
                        logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::STOP PROCESSING.");
                        break;
                    }

                    await Task.Delay(_telemetryInterval * 1000);
                }
            }
        }

        internal async Task SendDeviceToCloudErrorAsync(ModuleClient moduleClient, string deviceId, string moduleId, int interval, ILogger logger)
        {
            int counter = 1;
            string logPrefix = "error".BuildLogPrefix();
            string messageString = string.Empty;

            using (logger.BeginScope($"{logPrefix}::{ModuleSettings.ArtifactId}::ERROR MESSAGE (SENT BY THE DEVICE)"))
            {
                while (true)
                {
                    messageString = await _errorMessagingService.GetRandomizedMessageAsync(deviceId, moduleId);

                    var message = new Message(Encoding.ASCII.GetBytes(messageString));
                    message.Properties.Add("messagetype", "error");

                    // Add a custom application property to the message.
                    // An IoT hub can filter on these properties without access to the message body.
                    message.ContentType = "application/json";
                    message.ContentEncoding = "utf-8";

                    // Send the tlemetry message
                    await moduleClient.SendEventAsync(message);
                    counter++;

                    logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Sent message: {messageString}.");
                    logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::COUNTER: {counter}.");

                    if (_stopProcessing)
                    {
                        logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::STOP PROCESSING.");
                        break;
                    }

                    await Task.Delay(interval * 1000);
                }
            }
        }

        //Commissioning messages
        internal async Task SendDeviceToCloudCommissioningAsync(ModuleClient moduleClient, string deviceId, string moduleId, int interval, ILogger logger)
        {
            string logPrefix = "commissioning".BuildLogPrefix();
            int counter = 1;
            string messageString = string.Empty;

            using (logger.BeginScope($"{logPrefix}::{ModuleSettings.ArtifactId}::COMMISSIONING MESSAGE"))
            {
                while (true)
                {
                    messageString = await _commissioningMessagingService.GetRandomizedMessageAsync(deviceId, moduleId);

                    var message = new Message(Encoding.ASCII.GetBytes(messageString));
                    message.Properties.Add("messagetype", "commissioning");

                    // Add a custom application property to the message.
                    // An IoT hub can filter on these properties without access to the message body.
                    message.ContentType = "application/json";
                    message.ContentEncoding = "utf-8";

                    // Send the tlemetry message
                    await moduleClient.SendEventAsync(message);

                    logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Sent message: {messageString}.");
                    logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::COUNTER: {counter}.");

                    if (_stopProcessing)
                    {
                        logger.LogDebug($"{logPrefix}::STOP PROCESSING.");
                        break;
                    }

                    await Task.Delay(interval * 1000);
                }
            }
        }

        #endregion

        #region C2D
        #region Messages
        private async Task ReceiveC2DMessagesAsync(ModuleClient moduleClient, ModuleSettings settings, ILogger logger)
        {
            string logPrefix = "c2dmessages".BuildLogPrefix();

            await moduleClient.SetMessageHandlerAsync(C2DMessageHandler, null);
            logger.LogTrace($"{logPrefix}::{ModuleSettings.ArtifactId}::C2D MESSAGE reception handler registered.");
        }

        private async Task<MessageResponse> C2DMessageHandler(Message message, object context)
        {
            string logPrefix = "c2dmessages".BuildLogPrefix();

            _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Receiving cloud to device messages from service.");

            if (message != null)
            {
                _logger.LogInformation($"{logPrefix}::C2D MESSAGE RECEIVED:{JsonConvert.SerializeObject(message, Formatting.Indented)}.");
                return MessageResponse.Completed;
            }
            else
                return MessageResponse.None;
        }

        private async Task RegisterC2DDirectMethodsHandlersAsync(ModuleClient moduleClient, ModuleSettings settings, ILogger logger)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            try
            {
                // Create a handler for the direct method call

                await moduleClient.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null);
                logger.LogTrace($"{logPrefix}::{ModuleSettings.ArtifactId}::DIRECT METHOD SetTelemetryInterval registered.");

                await moduleClient.SetMethodHandlerAsync("Reboot", Reboot, null);
                logger.LogTrace($"{logPrefix}::{ModuleSettings.ArtifactId}::DIRECT METHOD Reboot registered.");

                await moduleClient.SetMethodHandlerAsync("OnOff", StartOrStop, null);
                logger.LogTrace($"{logPrefix}::{ModuleSettings.ArtifactId}::DIRECT METHOD OnOff registered.");

                await moduleClient.SetMethodHandlerAsync("ReadTwins", ReadTwinsAsync, null);
                logger.LogTrace($"{logPrefix}::{ModuleSettings.ArtifactId}::DIRECT METHOD ReadTwins registered.");

                await moduleClient.SetMethodHandlerAsync("GenericJToken", GenericJToken, null);
                logger.LogTrace($"{logPrefix}::{ModuleSettings.ArtifactId}::DIRECT METHOD GenericJToken registered.");

                await moduleClient.SetMethodHandlerAsync("Generic", Generic, null);
                logger.LogTrace($"{logPrefix}::{ModuleSettings.ArtifactId}::DIRECT METHOD Generic registered.");

                await moduleClient.SetMethodDefaultHandlerAsync(DefaultC2DMethodHandler, null);
                _logger.LogTrace($"{logPrefix}::{ModuleSettings.ArtifactId}::DIRECT METHOD Default handler registered.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{logPrefix}::{ModuleSettings.ArtifactId}::ERROR:RegisterC2DDirectMethodsHandlersAsync:{ex.Message}.");
            }
        }
        #endregion

        #region Direct Methods
        // Handle the direct method call
        //https://docs.microsoft.com/en-us/azure/iot-hub/quickstart-control-device-dotnet
        private Task<MethodResponse> SetTelemetryInterval(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            var data = Encoding.UTF8.GetString(methodRequest.Data);

            // Check the payload is a single integer value
            if (Int32.TryParse(data, out _telemetryInterval))
            {
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Telemetry interval set to {_telemetryInterval} seconds.");

                // Acknowledge the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            else
            {
                // Acknowledge the direct method call with a 400 error message
                string result = "{\"result\":\"Invalid parameter\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
        }

        private Task<MethodResponse> GenericJToken(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            var data = Encoding.UTF8.GetString(methodRequest.Data);
            var content = JToken.FromObject(data);

            if (content != null)
            {
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Generic call received: {JsonConvert.SerializeObject(content)}.");

                // Acknowledge the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            else
            {
                // Acknowledge the direct method call with a 400 error message
                string result = "{\"result\":\"Invalid parameter\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
        }

        private Task<MethodResponse> Generic(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            var data = Encoding.UTF8.GetString(methodRequest.Data);

            _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Generic call received: {data}");

            // Acknowledge the direct method call with a 200 success message
            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private Task<MethodResponse> Reboot(MethodRequest methodRequest, object userContext)
        {
            // In a production device, you would trigger a reboot scheduled to start after this method returns
            // For this sample, we simulate the reboot by writing to the console and updating the reported properties 
            RebootOrchestration();

            string result = "'Reboot command has been received and planified.'";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private async Task RebootOrchestration()
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            try
            {
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Reboot order received.");
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Stoping processes...");

                _stopProcessing = true;


                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Processes stopped.");
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Rebooting...");

                // Update device twin with reboot time. 
                TwinCollection reportedProperties, reboot, lastReboot, rebootStatus;
                lastReboot = new TwinCollection();
                reboot = new TwinCollection();
                reportedProperties = new TwinCollection();
                rebootStatus = new TwinCollection();

                lastReboot["lastReboot"] = DateTime.Now;
                reboot["reboot"] = lastReboot;
                reboot["rebootStatus"] = "rebooting";
                reportedProperties["iothubDM"] = reboot;

                await _moduleClient.UpdateReportedPropertiesAsync(reportedProperties);

                await Task.Delay(10000);

                // Update device twin with reboot time. 
                reboot["rebootStatus"] = "online";
                reportedProperties["iothubDM"] = reboot;

                await _moduleClient.UpdateReportedPropertiesAsync(reportedProperties);
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Reboot over and system runing again.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{logPrefix}::{ModuleSettings.ArtifactId}::ERROR::RebootOrchestration:{ex.Message}.");
            }
            finally
            {
                _stopProcessing = false;
            }
        }

        private Task<MethodResponse> StartOrStop(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::StartOrStop command has been received and planified.");

            if (methodRequest.Data == null)
                throw new ArgumentNullException("methodRequest.Data");

            JObject jData = JsonConvert.DeserializeObject<JObject>(methodRequest.DataAsJson);

            JArray settings = (JArray)jData["data"];

            //Send feedback
            string result = "'StartOrStop command has been received.'";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private Task<MethodResponse> ReadTwinsAsync(MethodRequest methodRequest, object userContext)
        {
            ReadTwins();

            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private Task<MethodResponse> DefaultC2DMethodHandler(MethodRequest methodRequest, object userContext)
        {
            string logPrefix = "c2ddirectmethods".BuildLogPrefix();

            _logger.LogWarning($"{logPrefix}::{ModuleSettings.ArtifactId}::WARNING::{methodRequest.Name} has been called but there is no registered specific method handler.");

            string message = $"Request direct method: {methodRequest.Name} but no specifif direct method handler.";

            //Send feedback
            var response = new
            {
                result = message,
                payload = methodRequest.Data != null ? methodRequest.DataAsJson : string.Empty
            };

            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response, Formatting.Indented)), 200));

        }
        #endregion
        #endregion

        #region Twins
        //https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-twin-getstarted
        internal async Task ReportConnectivityAsync()
        {
            string logPrefix = "c2dtwins".BuildLogPrefix();

            try
            {
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Sending connectivity data as reported property.");

                TwinCollection reportedProperties, connectivity;
                reportedProperties = new TwinCollection();
                connectivity = new TwinCollection();
                connectivity["type"] = "cellular";
                connectivity["signalPower"] = "low";
                reportedProperties["connectivity"] = connectivity;
                await _moduleClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{logPrefix}::{ModuleSettings.ArtifactId}::ERROR::ReportConnectivityAsync:{ex.Message}.");
            }
        }

        internal async Task GenericTwinReportedUpdateAsync(string deviceId, string sensorId, string propertyName, dynamic value)
        {
            string logPrefix = "c2dtwins".BuildLogPrefix();

            try
            {
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Sending generic reported property update:: {propertyName}-{value}.");

                TwinCollection reportedProperties, configuration;
                reportedProperties = new TwinCollection();
                reportedProperties[propertyName] = value;

                await _moduleClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{logPrefix}::{ModuleSettings.ArtifactId}::ERROR::GenericTwinReportedUpdateAsync:{ex.Message}.");
            }
        }

        internal async Task ReportFirwmareUpdateAsync(string firmwareVersion, string firmwareUrl)
        {
            string logPrefix = "c2dtwins".BuildLogPrefix();

            try
            {
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::Sending firmware update notification.");

                TwinCollection reportedProperties;
                reportedProperties = new TwinCollection();

                reportedProperties["newFirmwareVersion"] = firmwareVersion;
                reportedProperties["Ur"] = firmwareUrl;
                await _moduleClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{logPrefix}::{ModuleSettings.ArtifactId}::ERROR::ReportFirwmareUpdateAsync:{ex.Message}.");
            }
        }

        internal async Task ReadTwins()
        {
            string logPrefix = "c2dtwins".BuildLogPrefix();

            //Twins
            _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::TWINS::Reading...");

            Twin twin = await _moduleClient.GetTwinAsync();

            if (twin != null)
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::TWINS:: {JsonConvert.SerializeObject(twin, Formatting.Indented)}.");
            else
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::TWINS:: No twins available.");
        }

        private async Task OnDesiredPropertyChange(TwinCollection desiredproperties, object usercontext)
        {
            string logPrefix = "c2dtwins".BuildLogPrefix();

            _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::TWINS-PROPERTIES-DESIRED properties changes request notification.");

            if (desiredproperties != null)
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::TWINS-PROPERTIES-DESIRED::{JsonConvert.SerializeObject(desiredproperties, Formatting.Indented)}");
            else
                _logger.LogDebug($"{logPrefix}::{ModuleSettings.ArtifactId}::TWINS-PROPERTIES-DESIRED properties change is emtpy.");

        }
        #endregion
    }
}
