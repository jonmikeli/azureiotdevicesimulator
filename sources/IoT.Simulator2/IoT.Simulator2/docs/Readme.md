# Azure IOT Device Simulator
*v0.1*

This project has for purpose to help IoT developers in terms of efficiency and simplicity when it comes to simulating IoT devices.

Example of uses:
 - development tool for developers working in IoT cloud solutions
 - scalable IoT simulation platforms
 - fast and simple development of IoT devices

The project is a .NET Core 2.x console application that implements most important client IoT features (Azure IoT SDK). The implementation relies on what Azure IoT Client SDK capabilities.

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

- Details about **HOW the solution WORKS** are provided in the [help](Help.md) section.
- Details about **HOW the solution can be USED** are provided in the [how to](HowTo.md) section.
