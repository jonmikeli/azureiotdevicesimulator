using Microsoft.Azure.Devices;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace IoT.Simulator.Latency
{
    public static class IoTFunctions
    {
        [FunctionName("IoTLatencyTestHandler")]
        public static async Task Run([EventHubTrigger("latency", Connection = "eventHubListenConnectionString")] EventData eventData, ILogger log)
        {

            if (eventData != null)
            {
                string iotHubConnectionString = "[IoT Hub Owner connection string to be allowed to call C2D operations]";
                ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);

                if (serviceClient == null)
                    throw new Exception("Service client is null");

                try
                {
                    string myIoTHubMessage = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    if (string.IsNullOrEmpty(myIoTHubMessage))
                        throw new ArgumentNullException(nameof(myIoTHubMessage));

                    LatencyTestRequest latencyTestRequest = JsonConvert.DeserializeObject<LatencyTestRequest>(myIoTHubMessage);

                    if (latencyTestRequest == null)
                        throw new ArgumentException("No JObject has been found in the request.", nameof(latencyTestRequest));

                    //NOTE: the Direct Method call could generalized if the request contains the name of the Direct Method that needs to be called.
                    var methodInvocation = new CloudToDeviceMethod("LatencyTestCallback") { ResponseTimeout = TimeSpan.FromSeconds(30) };
                    methodInvocation.SetPayloadJson(latencyTestRequest.StartTimestamp.ToString());

                    // Invoke the direct method asynchronously and get the response from the simulated device.
                    var response = await serviceClient.InvokeDeviceMethodAsync(latencyTestRequest.DeviceId, methodInvocation);

                    log.LogDebug($"LATENCY: C2D message sent");

                    await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    log.LogError(e.Message);
                }
            }
        }

        private static long ConvertToTimeStamp(DateTime data)
        {
            return (long)(data.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }
    }
}
