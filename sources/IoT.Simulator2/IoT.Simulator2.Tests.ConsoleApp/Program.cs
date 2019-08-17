using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace IoT.Simulator2.Tests.ConsoleApp
{
    class Program
    {
        static string _connectionString = @"HostName=iotedgedev-iothub-d62a46.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=K0YH3iPMzkphkgKyakRWf32mc75T14JJxEGbgQ2jsfU=";

        static void Main(string[] args)
        {

            try
            {
                Console.WriteLine("Tag update test");
                while (true)
                {
                    TestUpdateNewTwin("D1").GetAwaiter();
                    Console.ReadKey();

                    TestUpdate("D1").GetAwaiter();
                    Console.ReadKey();

                    //TestReplace("D1");
                    //Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.ReadKey();
            }
        }

        private static async Task TestUpdate(string deviceId)
        {
            RegistryManager rm = RegistryManager.CreateFromConnectionString(_connectionString);
            Twin twin = await rm.GetTwinAsync(deviceId);

            if (twin != null)
            {
                twin.Tags["test"] = $"update:{DateTime.Now.ToLongTimeString()}";
                var result = await rm.UpdateTwinAsync(twin.DeviceId, twin, twin.ETag);
                Console.WriteLine($"Update done: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
            }
        }

        private static async Task TestUpdateNewTwin(string deviceId)
        {
            RegistryManager rm = RegistryManager.CreateFromConnectionString(_connectionString);
            Twin twin = await rm.GetTwinAsync(deviceId);            

            if (twin != null)
            {
                Twin newTwin = new Twin(deviceId);
                newTwin.ETag = twin.ETag;

                newTwin.Tags["test"] = $"updateNewTwin:{DateTime.Now.ToLongTimeString()}";
                var result = await rm.UpdateTwinAsync(newTwin.DeviceId, newTwin, newTwin.ETag);
                Console.WriteLine($"Update done: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
            }
        }

        private static async Task TestReplace(string deviceId)
        {
            RegistryManager rm = RegistryManager.CreateFromConnectionString(_connectionString);
            Twin twin = await rm.GetTwinAsync(deviceId);

            if (twin != null)
            {
                twin.Tags["test"] = $"replace:{DateTime.Now.Second}";
                var result = await rm.ReplaceTwinAsync(twin.DeviceId, twin, twin.ETag);
                Console.WriteLine($"Replace done: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
            }
        }
    }
}
