# Azure IoT Device Simulator
*v0.1*

This project has for purpose to help IoT developers and testers in terms of efficiency and simplicity when it comes to simulating IoT devices. This simulator implements different types of C2D/D2C flows between Microsoft IoT Hub and the simulated device.

[*Detailled information*](./sources/IoT.Simulator2/IoT.Simulator2/docs/Readme.md).

Example of uses:
 - development tool for developers working in IoT cloud solutions
 - tester tool in IoT-oriented projects
 - scalable IoT simulation platforms
 - fast and simple development of IoT devices

Technical information:
 - .NET Core 2.x
 - Microsoft Azure IoT SDK (Device capabilities, including IoT Hub modules)

*Azure IoT Device Simulator logs*

![Azure IoT Device Simulator Logs](sources/IoT.Simulator2/IoT.Simulator2/docs/images/AzureIoTDeviceSimulatorLos.gif)

## Global features:
 - device simulation
 - module simulation
 - JSON oriented device simulation configuration
 - JSON oriented module oriented configuration
 - no specific limitation in number of modules (only limited by IoT Hub constraints)
 - containerized
 - JSON oriented message definition
 - implementation of full IoT flows (C2D, D2C)

## Functional features
The solution simulates the functional features described below:
 - telemetry sent from a device
 - a device consisting of different modules (either functional or technical). Ex: device with a module for tempeterature/humidity/pressure/etc and another module to check presence or manage actions
 - firmware update over the air (FOTA)

## Detailed technical features
### D2C
#### Device level
 - IoT Messages
 - Twins (Reported)

#### Module level
 - IoT Messages
 - Twins (Reported)

### C2D
#### Device level
 - Twins (Desired)
 - Twins (Tags)
 - Direct Methods
 - Messages

#### Module level
 - Twins (Desired)
 - Twins (Tags)
 - Direct Methods
 - Messages

## More information
- Detailled [information](./sources/IoT.Simulator2/IoT.Simulator2/docs/Readme.md).
