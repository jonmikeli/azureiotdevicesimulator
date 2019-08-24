using Microsoft.Azure.Devices;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoT.Simulator2.Latency
{
    public static class IoTFunctions
    {
        [FunctionName("IoTLatencyTestHandler")]
        public static async Task Run([EventHubTrigger("latency", Connection = "eventHubListenConnectionString")] EventData[] events, ILogger log)
        {

            if (events != null && events.Count() > 0)
            {
                var exceptions = new List<Exception>();

                string iotHubConnectionString = "[IoT Hub Owner connection string to be allowed to call C2D operations]";
                ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);

                if (serviceClient == null)
                    throw new Exception("Service client is null");

                //NOTE: foreach could even be parallelized to improve performance
                foreach (EventData eventData in events)
                {
                    try
                    {
                        string myIoTHubMessage = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                        if (string.IsNullOrEmpty(myIoTHubMessage))
                            throw new ArgumentNullException(nameof(myIoTHubMessage));

                        LatencyTestRequest latencyTestRequest = JsonConvert.DeserializeObject<LatencyTestRequest>(myIoTHubMessage);

                        if (latencyTestRequest == null)
                            throw new ArgumentException("No JObject has been found in the request.", nameof(latencyTestRequest));

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
                        exceptions.Add(e);
                    }
                }

                // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.
                if (exceptions.Count > 1)
                    throw new AggregateException(exceptions);

                if (exceptions.Count == 1)
                    throw exceptions.Single();
            }
        }

        private static long ConvertToTimeStamp(DateTime data)
        {
            return (long)(data.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }
    }
}
