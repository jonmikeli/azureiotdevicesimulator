# Azure IoT Device Simulator - Latency Tests


This section describes a way to measure first level latency between a given device and the cloud solution (Microsoft Azure IoT Hub and Azure Function).

For details about how the simulator works, see the [help](Help.md) file.

## Why to measure latency?
Keeping a close eye on global performance is important in any solution.
This becomes even more important when it comes to IoT.

You will find here a simple solution to measure raw latencies between a given device and the Microsoft Azure IoT Hub where it has been registered.

## Solution
The solution resides on:
 - a specific message type (messageType = latency).
 - a specific IoT Hub route with and endpoint of type EventHub (to be configured in your Azure solution).
 - an Azure Function that listens to the EventHub and calls a Direct Method (IoT Hub feature) in order to send a response back to the device having created the latency test request.

 Thus, the device has the request and the response times, what allows him to math the latency.

 Simple and efficient.

 
## Latency message
The structure of the latency message is specific.

```json
{
 "deviceId":"",
 "messageType":"latency",
 "startTimestamp":
}
```

## Azure Function

You will find the Azure Function project [here](https://github.com/jonmikeli/azureiotdevicesimulator/tree/master/sources/IoT.Simulator/IoT.Simulator.AF) with the source code.

