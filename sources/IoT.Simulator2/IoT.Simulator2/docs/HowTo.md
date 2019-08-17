# Azure IoT Device Simulator - How To
*v0.1*

## How to use the IoT simulator?

The IoT Simulator has been containerized in order to simplify its delivery and use.
You can find the Docker image at [this location](https://hub.docker.com/r/jonmikeli/azureiotdevicesimulator).

If you need detailled documentation about what IoT Simulator is, you can find additional information at:
 - [Readme](Readme.md)
 - [Help](Help.md)

## How to get a Docker image?
### Prerequisites
In order to use a Docker container, you need all the prerequistes [Docker](https://www.docker.com/) could need in your system.

In addition, you will need an internet connection and allow the ports below:
 - 8883
 - 5671
 - 443
[Ports](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-protocols) required to communicate with Microsoft Azure IoT Hub.

Finally, you will need enough storage memory to download the Docker image and create one or more containers.

### Steps to follow
The IoT Simulator needs to basic things before starting it:
 - settings (need to be updated with connection settings)
 - message templates (included by default)

 Once those items are ready, a single command allows to start the simulator.

#### Settings
Settings are based on 3 files:
 - [appsettings.json](#appsettings.json)
 - [devicesettings.json](#devicesettings.json)
 - [modulessettings.json](#modulessettings.json)

For details and explainations, see [help](./Help.md).

> [!TIP]
> The solution takes into account **settings** depending on the environment.
> It can be set trough the environment variable ENVIRONMENT.
> The solution looks for settings files respecting the common pattern *file.ENVIRONMENT.json*.
> By default, the default setting files will be loaded first.

##### appsettings.json
This file allows to configure system related items (logs, etc).

**Production**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

**Development**
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

##### appsettings.json
This file allows to configure system related items (logs, etc).

**Production**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

**Development**
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
This file allows to configure system related items (logs, etc).

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
This file allows to configure system related items (logs, etc).

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

#### Message templates
You will find in this section the default templates of the messages sent by the simulator.
You are totally free to change the and adapt them to your needs.

> [!WARNING]
> Just keep in mind that many values are randomized by a service before sending the messages.
> This version of the simulator does not deal with dynamic JSON Schemas. So, if the message template is complete reviewed and new randomized properties are added, you will need to either update the existing message service or create yours and update the IoC/DI of the solution.

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

**Changing environment**
```cmd
dotnet IoT.Simulator2.dll --ENVIRONMENT=Development
```
