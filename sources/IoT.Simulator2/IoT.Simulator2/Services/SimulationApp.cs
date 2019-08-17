using IoT.Simulator2.Extensions;
using IoT.Simulator2.Models;
using IoT.Simulator2.Settings;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IoT.Simulator2.Services
{
    public class SimulationApp
    {
        private readonly ILogger<SimulationApp> _logger;

        private DeviceSettings _deviceSettings;
        private SimulationSettingsDevice _simulationSettings;
        private DeviceClient s_deviceClient;
        private string _deviceId;
        private string _iotHub;
        private int _telemetryInterval;
        private bool _stopProcessing = false;

        public SimulationApp(IOptions<DeviceSettings> deviceSettings, IOptions<SimulationSettingsDevice> simulationSettings, ILoggerFactory loggerFactory)
        {
            if (deviceSettings == null)
                throw new ArgumentNullException("deviceSettings");

            if (simulationSettings == null)
                throw new ArgumentNullException("simulationSettings");

            if (deviceSettings.Value == null)
                throw new ArgumentNullException("deviceSettings.Value", "No device configuration has been loaded");

            if (simulationSettings.Value == null)
                throw new ArgumentNullException("simulationSettings.Value", "No simulation configuration has been loaded");

            if (loggerFactory == null)
                throw new ArgumentNullException("loggerFactory", "No logger factory has been provided");

            _deviceSettings = deviceSettings.Value;
            _simulationSettings = simulationSettings.Value;

            _deviceId = _deviceSettings.DeviceId;
            _iotHub = _deviceSettings.HostName;

            _telemetryInterval = _simulationSettings.TelemetryFrecuency;

            _logger = loggerFactory.CreateLogger<SimulationApp>();
            _logger.LogDebug($"Logger loaded.");
        }

        private void CheckDeviceConnectionStringData(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("connectionString", "No IoT Hub connection string has been found");

            //Hostname
            string hostname = connectionString.ExtractValue("HostName");
            if (string.IsNullOrEmpty(hostname))
                throw new ArgumentNullException("hostname", "No hostname has been found within the connection string");

            _logger.LogTrace($"CheckDeviceConnectionStringData::Hostname: {hostname}");

            //DeviceId
            string deviceId = connectionString.ExtractValue("DeviceId");
            if (string.IsNullOrEmpty(deviceId))
                throw new ArgumentNullException("DeviceId", "No deviceId has been found within the connection string");

            _logger.LogTrace($"CheckDeviceConnectionStringData::DeviceId: {deviceId}");
        }

        public async Task LoadSimulationsAsync()
        {
            try
            {
                CheckDeviceConnectionStringData(_deviceSettings.ConnectionString);

                // Connect to the IoT hub using the MQTT protocol
                s_deviceClient = DeviceClient.CreateFromConnectionString(_deviceSettings.ConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
                if (_simulationSettings.EnableTwinPropertiesDesiredChangesNotifications)
                    await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChange, null);

                if (_simulationSettings.EnableC2DDirectMethods)
                    //Register C2D Direct methods handlers            
                    RegisterC2DDirectMethodsHandlersAsync();

                if (_simulationSettings.EnableC2DMessages)
                    //Start receiving C2D messages
                    ReceiveC2DMessagesAsync();

                if (_simulationSettings.EnableLatencyTests)
                    SendDeviceToCloudLatencyTestAsync(_deviceId, _simulationSettings.LatencyTestsFrecuency);

                if (_simulationSettings.EnableTelemetryMessages)
                    SendDeviceToCloudMessagesAsync(_deviceId); //interval is a global variable changed by processes

                if (_simulationSettings.EnableErrorMessages)
                    SendDeviceToCloudErrorAsync(_deviceId, _simulationSettings.ErrorFrecuency);

                if (_simulationSettings.EnableCommissioningMessages)
                    SendDeviceToCloudCommissioningAsync(_deviceId, _simulationSettings.CommissioningFrecuency);

                if (_simulationSettings.EnableReadingTwinProperties)
                {
                    //Twins
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("Retrieving twins");
                    Twin twin = await s_deviceClient.GetTwinAsync();
                    Console.WriteLine($"Device twins: {twin?.ToJson()}");
                    Console.ResetColor();
                }

                if (_simulationSettings.EnableFileUpload)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Building bulk upload messages");

                    Random rand = new Random();
                    DateTime dataStartDate = DateTime.Now.AddDays(-5);
                    List<MeasuredData> data = new List<MeasuredData>();

                    int i = 0;
                    while (i < _simulationSettings.NumberOfMessagesToUpload)
                    {
                        data.Add(new MeasuredData
                        {
                            Data = new DataItem[]
                            {
                            new DataItem { Timestamp = dataStartDate.AddMinutes(i*3+rand.Next(0,2)).AddSeconds(rand.Next(1,59)).TimeStamp(), PropertyName = "temperature", PropertyValue = (rand.NextDouble()*50).ToString(), PropertyUnit = "C" },
                            new DataItem { Timestamp = dataStartDate.AddMinutes(i*3+rand.Next(0,2)).AddSeconds(rand.Next(1,59)).TimeStamp(), PropertyName = "pressure", PropertyValue = rand.Next(900,1500).ToString(), PropertyUnit = "ba" },
                            new DataItem { Timestamp = dataStartDate.AddMinutes(i*3+rand.Next(0,2)).AddSeconds(rand.Next(1,59)).TimeStamp(), PropertyName = "humidity", PropertyValue = rand.Next(0,100).ToString(), PropertyUnit = "%" },
                            new DataItem { Timestamp = dataStartDate.AddMinutes(i*3+rand.Next(0,2)).AddSeconds(rand.Next(1,59)).TimeStamp(), PropertyName = "presence", PropertyValue = rand.Next(0,1).ToString(), PropertyUnit = "bool" }
                            },
                            DeviceId = _deviceId,
                            MessageType = "data",
                            SchemaVersion = "v1.0",
                            Timestamp = DateTime.UtcNow.TimeStamp()
                        });

                        i++;
                    }

                    Console.WriteLine("Uploading....");
                    await FileUploadAsync<MeasuredData>(_iotHub, _deviceId, data);
                    Console.WriteLine("First upload over");

                    await Task.Delay(240 * 1000);

                    Console.WriteLine("Uploading....");
                    await FileUploadAsync<MeasuredData>(_iotHub, _deviceId, data);
                    Console.WriteLine("Second upload over");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }


        #region D2C        
        private string RandomizeData(string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage))
                throw new ArgumentNullException(nameof(jsonMessage));

            string result = jsonMessage;

            JObject jobject = JObject.Parse(jsonMessage);

            if (jobject != null)
            {
                JToken jData;

                if (jobject.TryGetValue("data", out jData) && jData.Type == JTokenType.Array)
                {
                    foreach (var item in (JArray)jData)
                    {
                        if (item["wattsType"].Value<string>() == "At")
                        {
                            JToken jdataItems;
                            if (((JObject)item).TryGetValue("dataItems", out jdataItems))
                            {
                                Random r = new Random(DateTime.Now.Second);
                                foreach (var dataItem in (JArray)jdataItems)
                                {
                                    dataItem["timestamp"] = JValue.FromObject(DateTime.Now.TimeStamp());
                                    dataItem["value"] = JValue.FromObject(r.Next(150, 300).ToString());
                                }
                            }
                        }
                    }
                }

                return JsonConvert.SerializeObject(jobject, Formatting.Indented);
            }
            else return null;
        }

        // Async method to send simulated telemetry
        private async Task SendDeviceToCloudMessagesAsync(string deviceId)
        {
            // Initial telemetry values
            double minTemperature = 20;
            double minHumidity = 60;
            Random rand = new Random();
            int counter = 1;

            while (true)
            {
                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentAmbientTemperature = minTemperature + rand.NextDouble() * 15 - 2;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;

                string messageString = File.ReadAllText(@"./Messages/measureddata.json");

                if (string.IsNullOrEmpty(messageString))
                    throw new ArgumentNullException(nameof(messageString), "DATA: The message to send is empty or not found.");

                //Randomize data
                messageString = RandomizeData(messageString);

                var message = new Message(Encoding.UTF8.GetBytes(messageString));
                message.Properties.Add("messageType", "data");

                // Add a custom application property to the message.
                // An IoT hub can filter on these properties without access to the message body.
                //message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");
                message.ContentType = "application/json";
                message.ContentEncoding = "utf-8";

                // Send the tlemetry message
                await s_deviceClient.SendEventAsync(message);
                counter++;

                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
                Console.WriteLine("{0} > COUNTER: {1}", DateTime.Now, counter);
                Console.ResetColor();

                if (_stopProcessing)
                    break;

                await Task.Delay(_telemetryInterval * 1000);
            }
        }

        private async Task SendDeviceToCloudErrorAsync(string deviceId, int interval)
        {
            while (true)
            {
                string messageString = File.ReadAllText(@"./Messages/error.json");

                if (string.IsNullOrEmpty(messageString))
                    throw new ArgumentNullException(nameof(messageString), "ERROR: The message to send is empty or not found.");

                var message = new Message(Encoding.ASCII.GetBytes(messageString));
                message.Properties.Add("messagetype", "error");

                // Add a custom application property to the message.
                // An IoT hub can filter on these properties without access to the message body.
                message.ContentType = "application/json";
                message.ContentEncoding = "utf-8";

                // Send the tlemetry message
                await s_deviceClient.SendEventAsync(message);

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
                Console.ResetColor();

                if (_stopProcessing)
                    break;

                await Task.Delay(interval * 1000);
            }
        }

        //Commissioning messages
        private async Task SendDeviceToCloudCommissioningAsync(string deviceId, int interval)
        {
            while (true)
            {

                string messageString = File.ReadAllText(@"./Messages/commissioning.json");

                if (string.IsNullOrEmpty(messageString))
                    throw new ArgumentNullException(nameof(messageString), "COMMISSIONING: The message to send is empty or not found.");

                var message = new Message(Encoding.ASCII.GetBytes(messageString));
                message.Properties.Add("messagetype", "commissioning");

                // Add a custom application property to the message.
                // An IoT hub can filter on these properties without access to the message body.
                message.ContentType = "application/json";
                message.ContentEncoding = "utf-8";

                // Send the tlemetry message
                await s_deviceClient.SendEventAsync(message);
                var currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
                Console.ForegroundColor = currentColor;

                if (_stopProcessing)
                    break;

                await Task.Delay(interval * 1000);
            }
        }

        //Latency tests
        private async Task SendDeviceToCloudLatencyTestAsync(string deviceId, int interval)
        {
            while (true)
            {

                var data = new 
                {
                    deviceId = deviceId,
                    messageType = "latency",
                    starttimestamp = DateTime.UtcNow.TimeStamp()
                };


                var messageString = JsonConvert.SerializeObject(data);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));
                message.Properties.Add("messagetype", "latency");
                message.ContentType = "application/json";
                message.ContentEncoding = "utf-8";

                // Send the tlemetry message
                await s_deviceClient.SendEventAsync(message);

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
                Console.ResetColor();

                if (interval <= 0)
                    break;

                if (_stopProcessing)
                    break;

                await Task.Delay(interval * 1000);
            }
        }

        //https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-file-upload
        private async Task FileUploadAsync<T>(string iotHub, string deviceId, List<T> dataToUpload)
        {
            try
            {
                if (dataToUpload == null)
                    throw new ArgumentNullException("dataToUpload");

                string sasUriEndPoint = $@"https://{iotHub}/devices/{deviceId}/files"; //1
                string endUploadEndpoint = $@"https://{iotHub}/devices/{deviceId}/files/notifications"; //2

                string localFileName = $"{DateTime.UtcNow.TimeStamp()}-{deviceId}.json";


                File.WriteAllText(localFileName, JsonConvert.SerializeObject(dataToUpload));

                using (var sourceData = new FileStream(localFileName, FileMode.Open))
                {
                    await s_deviceClient.UploadToBlobAsync(localFileName, sourceData);
                }
                
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

        }
        #endregion

        #region C2D
        #region Messages
        private async Task ReceiveC2DMessagesAsync()
        {
            Console.WriteLine("\nReceiving cloud to device messages from service");
            while (true)
            {
                Message receivedMessage = await s_deviceClient.ReceiveAsync();
                if (receivedMessage == null) continue;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Received message: {0}", Encoding.ASCII.GetString(receivedMessage.GetBytes()));
                Console.ResetColor();

                await s_deviceClient.CompleteAsync(receivedMessage);
            }
        }

        private async Task RegisterC2DDirectMethodsHandlersAsync()
        {
            // Create a handler for the direct method call
            s_deviceClient.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null).Wait();
            s_deviceClient.SetMethodHandlerAsync("LatencyTestCallback", LatencyTestCallback, null).Wait();
            s_deviceClient.SetMethodHandlerAsync("SendLatencyTest", LatencyTestCallback, null).Wait();
            s_deviceClient.SetMethodHandlerAsync("Reboot", Reboot, null).Wait();
            s_deviceClient.SetMethodHandlerAsync("OnOff", StartOrStop, null).Wait();

            s_deviceClient.SetMethodHandlerAsync("ReadTwins", ReadTwinsAsync, null).Wait();
            s_deviceClient.SetMethodHandlerAsync("GenericJToken", GenericJToken, null).Wait();
            s_deviceClient.SetMethodHandlerAsync("Generic", Generic, null).Wait();
        }
        #endregion

        #region Direct Methods
        // Handle the direct method call
        //https://docs.microsoft.com/en-us/azure/iot-hub/quickstart-control-device-dotnet
        private Task<MethodResponse> SetTelemetryInterval(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);

            // Check the payload is a single integer value
            if (Int32.TryParse(data, out _telemetryInterval))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Telemetry interval set to {0} seconds", data);
                Console.ResetColor();

                // Acknowlege the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            else
            {
                // Acknowlege the direct method call with a 400 error message
                string result = "{\"result\":\"Invalid parameter\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
        }

        private Task<MethodResponse> GenericJToken(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);
            var content = JToken.FromObject(data);

            // Check the payload is a single integer value
            if (content != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Generic call received: {JsonConvert.SerializeObject(content)}");
                Console.ResetColor();

                // Acknowlege the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            else
            {
                // Acknowlege the direct method call with a 400 error message
                string result = "{\"result\":\"Invalid parameter\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
        }

        private Task<MethodResponse> Generic(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);


            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Generic call received: {data}");
            Console.ResetColor();

            // Acknowlege the direct method call with a 200 success message
            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private Task<MethodResponse> LatencyTestCallback(MethodRequest methodRequest, object userContext)
        {
            var callbackTimestamp = DateTime.UtcNow.TimeStamp();
            var data = Encoding.UTF8.GetString(methodRequest.Data);
            long initialtimestamp = 0;

            // Check the payload is a single integer value
            Console.ForegroundColor = ConsoleColor.Cyan;
            if (Int64.TryParse(data, out initialtimestamp))
            {
                Console.WriteLine($"Latency test callback: latency: {callbackTimestamp - initialtimestamp} s");
                Console.WriteLine(data);
            }
            Console.ResetColor();

            // Acknowlege the direct method call with a 200 success message
            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\",\"latency\":" + (callbackTimestamp - initialtimestamp).ToString() + "}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private Task<MethodResponse> SendLatencyTest(MethodRequest methodRequest, object userContext)
        {
            SendDeviceToCloudLatencyTestAsync(_deviceId, 0);

            // Acknowlege the direct method call with a 200 success message
            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"";
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
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Reboot order received.");
                Console.WriteLine("Stoping processes...!");

                _stopProcessing = true;


                Console.WriteLine("Processes stopped.");
                Console.WriteLine("Rebooting....");

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

                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);

                await Task.Delay(10000);

                // Update device twin with reboot time. 
                reboot["rebootStatus"] = "online";
                reportedProperties["iothubDM"] = reboot;

                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
                Console.WriteLine("Reboot over and system runing again");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
            finally
            {
                _stopProcessing = false;
                Console.ResetColor();
            }
        }

        private Task<MethodResponse> StartOrStop(MethodRequest methodRequest, object userContext)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Start command has been received and planified.");

            if (methodRequest.Data == null)
                throw new ArgumentNullException("methodRequest.Data");

            JObject jData = JsonConvert.DeserializeObject<JObject>(methodRequest.DataAsJson);

            string sensorId = jData["S1"].Value<string>();
            string configurationVersion = jData["configurationVersion"].Value<string>();
            JArray settings = (JArray)jData["data"];

            foreach (JObject item in settings)
            {
                ReportConfigurationUpdateAsync(_deviceId, sensorId, configurationVersion, item["wattsType"].Value<string>(), item["wattsTypeValue"].Value<int>());
            }

            Console.ResetColor();

            //Send feedback

            string result = "'StartOrStop command has been received.'";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private Task<MethodResponse> ReadTwinsAsync(MethodRequest methodRequest, object userContext)
        {
            ReadTwins();

            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("Twins read"), 400));
        }
        #endregion
        #endregion

        #region Twins
        //https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-twin-getstarted
        public async Task ReportConnectivityAsync()
        {
            try
            {
                Console.WriteLine("Sending connectivity data as reported property");

                TwinCollection reportedProperties, connectivity;
                reportedProperties = new TwinCollection();
                connectivity = new TwinCollection();
                connectivity["type"] = "cellular";
                reportedProperties["connectivity"] = connectivity;
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        public async Task ReportUpdateStatusAsync(string type, string status, string version)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Sending update status data as reported property");
                Console.ResetColor();

                TwinCollection reportedProperties, updateStatus;
                reportedProperties = new TwinCollection();
                updateStatus = new TwinCollection();
                updateStatus["type"] = type;
                updateStatus["status"] = status;
                updateStatus["targetVersion"] = version;
                reportedProperties["updateStatus"] = updateStatus;
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        public async Task ReportConfigurationUpdateAsync(string deviceId, string sensorId, string configurationVersion, string wattsType, dynamic wattsTypeValue)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Sending configuration update as reported property");
                Console.ResetColor();

                TwinCollection reportedProperties, configuration;
                reportedProperties = new TwinCollection();
                configuration = new TwinCollection();
                configuration["Id_deviceId"] = deviceId;
                configuration["S1"] = sensorId;
                configuration["configurationVersion"] = configurationVersion;
                configuration["data"] = JObject.FromObject(
                        new { wattsType = wattsType, wattsTypeValue = wattsTypeValue }
                    );
                //configuration["data"] = JsonConvert.SerializeObject(
                //    new List<dynamic> {
                //        new { wattsType = wattsType, value = wattsTypeValue }
                //    }
                //    );

                reportedProperties["configuration"] = configuration;
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        public async Task GenericTwinReportedUpdateAsync(string deviceId, string sensorId, string propertyName, dynamic value)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Sending generic reported property update:: id_deviceId:{deviceId}-S1:{sensorId}-{propertyName}-{value}");
                Console.ResetColor();

                TwinCollection reportedProperties, configuration;
                reportedProperties = new TwinCollection();
                reportedProperties[propertyName] = value;

                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        public async Task ReportConfigurationUpdateAsync(string deviceId, string sensorId, string configurationVersion, JObject data)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Sending configuration update as reported property");
                Console.ResetColor();

                TwinCollection reportedProperties, configuration;
                reportedProperties = new TwinCollection();
                configuration = new TwinCollection();
                configuration["Id_deviceId"] = deviceId;
                configuration["S1"] = sensorId;
                configuration["configurationVersion"] = configurationVersion;
                configuration["data"] = data;

                reportedProperties["configuration"] = configuration;
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        public async Task ReportFirwmareUpdateAsync(string firmwareVersion, string firmwareUrl)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Sending firmware update notification");
                Console.ResetColor();

                TwinCollection reportedProperties;
                reportedProperties = new TwinCollection();

                reportedProperties["Sv"] = firmwareVersion;
                reportedProperties["Vs"] = firmwareVersion;
                reportedProperties["Ur"] = firmwareUrl;
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        public async Task ReportLoadingConfigurationUpdateAsync(string deviceId, string previousA1, string newA1)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Load installation notification has been received and processed. Sending request reception notification...");
                Console.ResetColor();

                TwinCollection reportedProperties, loadInstallation;
                reportedProperties = new TwinCollection();
                loadInstallation = new TwinCollection();
                loadInstallation["previousA1"] = previousA1;
                loadInstallation["newA1"] = newA1;

                reportedProperties["loadInstallation"] = loadInstallation;
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        public async Task ReadTwins()
        {
            //Twins
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Retrieving twins");
            Twin twin = await s_deviceClient.GetTwinAsync();
            Console.WriteLine($"Device twins: {twin?.ToJson()}");
            Console.ResetColor();
        }

        private async Task OnDesiredPropertyChange(TwinCollection desiredproperties, object usercontext)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("TWINS-PROPERTIES-DESIRED properties changes request notification");
            Console.WriteLine(JsonConvert.SerializeObject(desiredproperties));

            if (desiredproperties.Contains("Vs"))
            {
                Console.WriteLine("FIRMWARE");
                Console.WriteLine("Firmware update request has been detected through desired properties.");
                Console.WriteLine($"Target version: {desiredproperties["Vs"].ToString()}");
                Console.WriteLine($"Target version: {desiredproperties["Ur"].ToString()}");

                JToken configuration = desiredproperties["Vs"];
                ReportFirwmareUpdateAsync(configuration.Value<string>(), desiredproperties["Ur"].ToString());
            }

            if (desiredproperties.Contains("configuration") && desiredproperties["configuration"] != null)
            {
                Console.WriteLine("CONFIGURATION");
                Console.WriteLine("Configuration changes request have been detected.");

                JObject configuration = desiredproperties["configuration"];

                string deviceId = string.Empty;
                string sensorId = string.Empty;
                string configurationVersion = string.Empty;
                string wattsType = string.Empty;
                string wattsTypeValue = string.Empty;

                if (configuration.ContainsKey("Id_deviceId"))
                    deviceId = configuration["Id_deviceId"].Value<string>();
                else
                    throw new ArgumentNullException(nameof(deviceId));

                if (configuration.ContainsKey("S1"))
                    sensorId = configuration["S1"].Value<string>();
                else
                    throw new ArgumentNullException(nameof(sensorId));

                if (configuration.ContainsKey("configurationVersion"))
                    configurationVersion = configuration["configurationVersion"].Value<string>();
                else
                    throw new ArgumentNullException(nameof(configurationVersion));

                if (!configuration.ContainsKey("data"))
                    throw new ArgumentNullException(nameof(deviceId));

                if (configuration["data"].Type == JTokenType.Array)
                {
                    JArray data = (JArray)configuration["data"];


                }
                else if (configuration["data"].Type == JTokenType.Object)
                {
                    JObject data = (JObject)configuration["data"];

                    //WARNING: In contrast with Direct Methods, Twins do not allow arrows in parameters.
                    //We need to adapt processing.

                    JToken jWattsTypeValue;
                    if (data.TryGetValue("wattsTypeValue", out jWattsTypeValue))
                    {
                        var wattsTypeValueType = data["wattsTypeValue"].Type;

                        if (wattsTypeValueType == JTokenType.Integer)
                        {
                            Console.WriteLine($"Settings updated: {data["wattsType"].Value<string>()} : {data["wattsTypeValue"].Value<int>()}");
                            ReportConfigurationUpdateAsync(deviceId, sensorId, configurationVersion, data["wattsType"].Value<string>(), data["wattsTypeValue"].Value<int>());
                        }
                        else if (wattsTypeValueType == JTokenType.String)
                        {
                            Console.WriteLine($"Settings updated: {data["wattsType"].Value<string>()} : {data["wattsTypeValue"].Value<string>()}");
                            ReportConfigurationUpdateAsync(deviceId, sensorId, configurationVersion, data["wattsType"].Value<string>(), data["wattsTypeValue"].Value<string>());
                        }
                        else if (wattsTypeValueType == JTokenType.Object)
                        {
                            Console.WriteLine($"Settings updated: {data["wattsType"].Value<string>()} : {data["wattsTypeValue"].ToString()}");
                            ReportConfigurationUpdateAsync(deviceId, sensorId, configurationVersion, data["wattsType"].Value<string>(), data["wattsTypeValue"]);
                        }
                    }
                    else
                    {
                        //FLATENED DATA
                        Console.WriteLine($"Settings updated: {data.ToString()}");
                        ReportConfigurationUpdateAsync(deviceId, sensorId, configurationVersion, data);
                    }

                    Console.WriteLine($"Settings updated and notification sent to IoT Hub");
                }
            }

            //New admin
            if (desiredproperties.Contains("loadInstallation"))
            {
                Console.WriteLine($"NEW ADMINISTRATOR {desiredproperties["loadInstallation"]["newA1"].ToString()}");
                ReportLoadingConfigurationUpdateAsync(_deviceId, desiredproperties["loadInstallation"]["previousA1"].ToString(), desiredproperties["loadInstallation"]["newA1"].ToString());
                Console.WriteLine($"Settings updated and notification sent to IoT Hub");
            }

            if (desiredproperties.Contains("testUpdate"))
            {
                GenericTwinReportedUpdateAsync(_deviceId, "", "testUpdate", desiredproperties["testUpdate"]);
            }

            if (desiredproperties.Contains("Ak"))
            {
                GenericTwinReportedUpdateAsync(_deviceId, "", "Ak", desiredproperties["Ak"]);
            }

            Console.ResetColor();
        }
        #endregion
    }
}
