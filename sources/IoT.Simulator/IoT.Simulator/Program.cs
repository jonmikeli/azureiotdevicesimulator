using IoT.Simulator.Exceptions;
using IoT.Simulator.Services;
using IoT.Simulator.Settings;
using IoT.Simulator.Tools;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;

namespace IoT.Simulator
{
    class Program
    {
        private static DeviceClient _deviceClient;

        // The device connection string to authenticate the device with your IoT hub.
        // Using the Azure CLI:
        // az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id MyDotnetDevice --output table
        private static string _iotHubConnectionString;
        private static string _environmentName;

        public static IConfiguration Configuration { get; set; }

        private static void Main(string[] args)
        {

            Console.WriteLine("=======================================================================");
            Console.WriteLine(AssemblyInformationHelper.HeaderMessage);
            Console.WriteLine("=======================================================================");
            Console.WriteLine(">> Loading configurations....");

            try
            {
                //Configuration
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("devicesettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("modulessettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();

                _environmentName = Environment.GetEnvironmentVariable("ENVIRONMENT");

                if (string.IsNullOrWhiteSpace(_environmentName))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No environment platform has been found. Default setting: Development.");
                    _environmentName = "Development";
                    Console.ResetColor();
                }

                try
                {
                    ConfigurationHelpers.CheckEnvironmentConfigurationFiles(_environmentName);
                }
                catch (MissingEnvironmentConfigurationFileException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                    Console.WriteLine("Execution will continue with default settings in appsettings.json, devicesettings.json and modulessettings.json.");
                }

                builder.AddJsonFile($"appsettings.{_environmentName}.json", optional: true, reloadOnChange: true);
                builder.AddJsonFile($"devicesettings.{_environmentName}.json", optional: true, reloadOnChange: true);
                builder.AddJsonFile($"modulessettings.{_environmentName}.json", optional: true, reloadOnChange: true);


                Configuration = builder.Build();

                //Service provider and DI
                IServiceCollection services = new ServiceCollection();

                ConfigureServices(services);

                var deviceSettings = Configuration.Get<DeviceSettings>();
                if (deviceSettings == null)
                    throw new ArgumentException("No device settings have been configured.");

                if (deviceSettings.SimulationSettings == null)
                    throw new ArgumentException("No device simulation settings have been configured.");

                if (deviceSettings.SimulationSettings.EnableDevice || deviceSettings.SimulationSettings.EnableModules)
                    //If any of the simulators is enabled, messaging services will be required to build the messages.
                    RegisterMessagingServices(services);

                if (deviceSettings.SimulationSettings.EnableDevice)
                    RegisterDeviceSimulators(services);

                if (deviceSettings.SimulationSettings.EnableModules)
                    RegisterModuleSimulators(deviceSettings, services);

                IServiceProvider serviceProvider = services.BuildServiceProvider();

                //Logger
                var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();
                logger.LogDebug("PROGRAM::Settings, DI and logger configured and ready to use.");

                //Simulators
                if (!deviceSettings.SimulationSettings.EnableDevice && !deviceSettings.SimulationSettings.EnableModules)
                    logger.LogDebug("PROGRAM:: No simulator has been configured.");
                else
                {
                    if (deviceSettings.SimulationSettings.EnableDevice)
                        StartDevicesSimulators(serviceProvider, logger);

                    if (deviceSettings.SimulationSettings.EnableModules)
                        StartModulesSimulators(serviceProvider, logger);
                }

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
            finally
            {
                Console.ReadLine();
            }
        }


        //logging
        //https://andrewlock.net/using-dependency-injection-in-a-net-core-console-application/
        static void ConfigureServices(IServiceCollection services)
        {
            if (services != null)
            {
                services.AddLogging(
                    loggingBuilder =>
                    {
                        loggingBuilder.ClearProviders();

                        //log level configuration
                        var loggingConfiguration = Configuration.GetSection("Logging");
                        loggingBuilder.AddConfiguration(loggingConfiguration);

                        if (_environmentName != "Production")
                        {
                            loggingBuilder.AddConsole();
                            loggingBuilder.AddDebug();
                        }
                    }
                    );

                services.AddOptions();

                services.Configure<AppSettings>(Configuration.GetSection(nameof(AppSettings)));
                services.Configure<DeviceSettings>(Configuration);
                services.Configure<ModulesSettings>(Configuration);

            }
        }

        static void RegisterDeviceSimulators(IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<ISimulationService, DeviceSimulationService>();
        }

        static void RegisterMessagingServices(IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddTransient<ITelemetryMessageService, SimpleTelemetryMessageService>();
            services.AddTransient<IErrorMessageService, SimpleErrorMessageService>();
            services.AddTransient<ICommissioningMessageService, SimpleCommissioningMessageService>();
        }

        static void RegisterModuleSimulators(DeviceSettings deviceSettings, IServiceCollection services)
        {
            if (deviceSettings == null)
                throw new ArgumentNullException(nameof(deviceSettings));

            if (deviceSettings.SimulationSettings == null)
                throw new ArgumentNullException("No device simulation configuration has been configured.");

            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (deviceSettings.SimulationSettings.EnableModules)
            {
                var modules = Configuration.Get<ModulesSettings>();
                if (modules != null && modules.Modules != null && modules.Modules.Any())
                {
                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    if (serviceProvider == null)
                        throw new ApplicationException("IServiceProvider has not been resolved.");

                    ILoggerFactory loggerFactory = serviceProvider.GetService<ILoggerFactory>();

                    if (loggerFactory == null)
                        throw new ApplicationException("ILoggerFactory has not been resolved.");

                    foreach (var item in modules.Modules)
                    {
                        var simulator = new ModuleSimulationService(
                            item,
                            item.SimulationSettings,
                            serviceProvider.GetService<ITelemetryMessageService>(),
                            serviceProvider.GetService<IErrorMessageService>(),
                            serviceProvider.GetService<ICommissioningMessageService>(),
                            loggerFactory);

                        services.AddSingleton<IModuleSimulationService, ModuleSimulationService>(iServiceProvider => simulator);
                    }
                }
            }
        }

        static void StartDevicesSimulators(IServiceProvider serviceProvider, ILogger logger)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            var simulators = serviceProvider.GetServices<ISimulationService>();
            if (simulators != null && simulators.Any())
            {
                foreach (var item in simulators)
                {
                    item.InitiateSimulationAsync();
                }

                logger.LogDebug($"DEVICES: {simulators.Count()} device simulator(s) initialized and running.");
            }
        }

        static void StartModulesSimulators(IServiceProvider serviceProvider, ILogger logger)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            var simulators = serviceProvider.GetServices<IModuleSimulationService>();
            if (simulators != null && simulators.Any())
            {
                foreach (var item in simulators)
                {
                    item.InitiateSimulationAsync();
                }

                logger.LogDebug($"MODULES: {simulators.Count()} module simulator(s) initialized and running.");
            }
        }
    }
}
