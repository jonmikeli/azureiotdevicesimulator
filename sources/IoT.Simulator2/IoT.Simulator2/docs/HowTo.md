# Azure IoT Device Simulator - How To


## How to use the Azure IoT Device Simulator?

The IoT simulator has been containerized in order to simplify its delivery and use.
You can find the Docker image at [this location](https://hub.docker.com/r/jonmikeli/azureiotdevicesimulator).

If you need or prefer the binary format, you can use the source code to compile the application and use it as a regular .NET Core 2.2.x Console application.

If you need detailed documentation about what Azure IoT Device Simulator is, you can find additional information at:
 - [Readme](../../../../Readme.md)
 - [Help](Help.md)


## How to get a Docker image?
### Prerequisites
In order to use a Docker container, you need to check [Docker](https://www.docker.com/) prerequisites.

Do not forget you will need an internet connection with specific open ports:
 - 8883
 - 5671
 - 443
[Ports](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-protocols) required to communicate with Microsoft Azure IoT Hub.

Finally, you will need enough storage memory to download the Docker image and create your containers.


### Steps to follow
The Azure IoT Device Simulator needs two basic things before starting:
 - settings (need to be updated with proper connection settings)
 - message templates (included by default)

 Once those items are ready, a single command allows starting the application.

#### Settings
Settings are based on 3 files:
 - [appsettings.json](#####appsettings.json)
 - [devicesettings.json](#####devicesettings.json)
 - [modulessettings.json](#####modulessettings.json)

For details and explanations, see [help](Help.md).

> [!TIP]
> 
> The solution takes into account **settings** depending on the environment.
> It can be set trough the environment variable ENVIRONMENT.
> The solution looks for settings files following the pattern *file.ENVIRONMENT.json* (similar to transformation files).
> Default setting files will be loaded first in case no environment file is found.

##### appsettings.json
This file allows configuring system related items (logs, etc).

**Release**

Minimal logs settings.
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

**Development (appsettings.Development.json)**

Detailed logs settings.
```json
{
  "Logging": {
    "Debug": {
      "LogLevel": {
        "Default": "Trace"
      }
    },
    "Console": {
      "IncludeScopes": true,
      "LogLevel": {
        "Default": "Trace"
      }
    },
    "LogLevel": {
      "Default": "Trace",
      "System": "Trace",
      "Microsoft": "Trace"
    }
  }
}
```

##### devicesettings.json
This file allows configuring device simulation settings.

```json
{
  "connectionString": "[IOT HUB NAME].azure-devices.net;DeviceId=[DEVIVE ID];SharedAccessKey=[SHARED KEY]",
  "simulationSettings": {
    "enableLatencyTests": false,
    "latencyTestsFrecuency": 30,
    "enableDevice": true,
    "enableModules": false,
    "enableTelemetryMessages": true,
    "telemetryFrecuency": 10,
    "enableErrorMessages": false,
    "errorFrecuency": 20,
    "enableCommissioningMessages": false,
    "commissioningFrecuency": 30,
    "enableTwinReportedMessages": false,
    "twinReportedMessagesFrecuency": 60,
    "enableReadingTwinProperties": false,
    "enableC2DDirectMethods": true,
    "enableC2DMessages": true,
    "enableTwinPropertiesDesiredChangesNotifications": true
  }
}

```

##### modulessettings.json
This file allows configuring module(s) simulation settings.

```json
{
 "modules":[
    {
      "connectionString": "[IOT HUB NAME].azure-devices.net;DeviceId=[DEVIVE ID];ModuleId=[MODULE ID];SharedAccessKey=[SHARED KEY]",
      "simulationSettings": {
        "enableLatencyTests": false,
        "latencyTestsFrecuency": 10,
        "enableTelemetryMessages": true,
        "telemetryFrecuency": 20,
        "enableErrorMessages": false,
        "errorFrecuency": 30,
        "enableCommissioningMessages": false,
        "commissioningFrecuency": 60,
        "enableTwinReportedMessages": false,
        "twinReportedMessagesFrecuency": 60,
        "enableReadingTwinProperties": true,
        "enableC2DDirectMethods": true,
        "enableC2DMessages": true,
        "enableTwinPropertiesDesiredChangesNotifications": true
      }
    }
  ]
}

```


> [!IMPORTANT]

> Do not forget to set your own values for `connectionString`. 



#### Message templates
You will find in this section the default templates of the messages sent by the simulator.
You are totally free to change and adapt them to your needs.

> [!WARNING]
> 
> This first version includes a dependency between message templates and message service implementation (randomized values and ID properties).
> For that reason, if the message template is completely reviewed and new randomized properties are added, you will need to either update the existing message service or create yours and update the IoC/DI settings.

##### Measured data / telemetry message
```json
{
  "deviceId": "",
  "moduleId": "",
  "timestamp": 0,
  "schemaVersion": "v1.0",
  "messageType": "data",
  "data": [
    {
      "timestamp": 0,
      "propertyName": "P1",
      "propertyValue": 35,
      "propertyUnit": "T",
      "propertyDivFactor": 1
    },
    {
      "timestamp": 0,
      "propertyName": "P2",
      "propertyValue": 1566,
      "propertyUnit": "U",
      "propertyDivFactor": 10
    }
  ]
}
```

##### Error message
```json
{
  "deviceId": "",
  "moduleId": "",
  "messageType": "error",
  "errorCode": "code",
  "errorSeverity": "severity",
  "errorStatus": "status",
  "timestamp": 13456
}
```

##### Commissioning message
```json
{
  "deviceId": "",
  "messageType": "commissioning",
  "timestamp": 13456,
  "userId": "",
  "building": {
    "buildingId": "",
    "floor": "",
    "departmentId": "",
    "roomId": null
  }
} 
```

#### Commands
**Regular**
```cmd
dotnet IoT.Simulator2.dll
```

**Changing the environment**

Linux
```cmd
export ENVIRONMENT=Development
dotnet IoT.Simulator2.dll
```

Windows
```cmd
set ENVIRONMENT=Development
dotnet IoT.Simulator2.dll
```
