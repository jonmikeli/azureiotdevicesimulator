# IOT Simulator documentation - Latency Tests
*v0.1, 13/08/2019*

This section describes a way to measure first level latency between a given device and the cloud solution.

For details about how the simulator works, see the [help](Help.md) file.

## Why to measure latency?
Keeping a close eye on global performance is important in any solution.
This becomes even more important when it comes to IoT and distributed solutions.

In addition, many IoT functional use cases require very fast responses.

Microsoft Azure offers products/solutions/services with extremely high performance levels. This being said, controlling time responses by our own, specially when it comes to tailored architectures or personal developments, it is vital.

You will find here a simple solution to measure raw latencies between a given device and the IoT Hub where it has been registered.

## Solution
The solution resides on:
 - a specific message type (messageType = latency).
 - a specific IoT Hub route with and endpoint of type EventHub.
 - an Azure Function that listens to the EventHub and calls a Direct Method (IoT Hub feature) in order to send a response to the device having created the latency test message.

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

You will find the Azure Function project [here](https://github.com/jonmikeli/azureiotdevicesimulator/tree/master/sources/IoT.Simulator2/IoT.Simulator2.AF).

