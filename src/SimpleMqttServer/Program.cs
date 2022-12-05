// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   The main program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleMqttServer;

/// <summary>
/// The main program.
/// </summary>
public class Program
{
    /// <summary>
    /// The configuration.
    /// </summary>
    private static IConfigurationRoot? config;

    /// <summary>
    /// Gets the environment name.
    /// </summary>
    public static string EnvironmentName => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

    /// <summary>
    /// Gets or sets the MQTT service configuration.
    /// </summary>
    public static MqttServiceConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// The service name.
    /// </summary>
    public static AssemblyName ServiceName => Assembly.GetExecutingAssembly().GetName();

    /// <summary>
    /// The main method.
    /// </summary>
    /// <param name="args">Some arguments.</param>
    /// <returns>The result code.</returns>
    public static async Task<int> Main(string[] args)
    {
        ReadConfiguration();
        SetupLogging();

        try
        {
            Log.Information("Starting {ServiceName}, Version {Version}...", ServiceName.Name, ServiceName.Version);
            var currentLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            await CreateHostBuilder(args, currentLocation!).Build().RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly.");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }

        return 0;
    }

    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <param name="currentLocation">The current assembly location.</param>
    /// <returns>A new <see cref="IHostBuilder"/>.</returns>
    private static IHostBuilder CreateHostBuilder(string[] args, string currentLocation) =>
        Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(
                webBuilder =>
                {;
                    webBuilder.UseContentRoot(currentLocation);
                    webBuilder.UseStartup<Startup>();
                })
            .UseSerilog()
            .UseWindowsService()
            .UseSystemd();

    /// <summary>
    /// Reads the configuration.
    /// </summary>
    private static void ReadConfiguration()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json", false, true);

        if (!string.IsNullOrWhiteSpace(EnvironmentName))
        {
            var appsettingsFileName = $"appsettings.{EnvironmentName}.json";

            if (File.Exists(appsettingsFileName))
            {
                configurationBuilder.AddJsonFile(appsettingsFileName, false, true);
            }
        }

        config = configurationBuilder.Build();
        config.Bind(ServiceName.Name ?? "SimpleMqttServer", Configuration);
    }

    /// <summary>
    /// Setup the logging.
    /// </summary>
    private static void SetupLogging()
    {
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithMachineName()
            .WriteTo.Console();

        if (EnvironmentName != "Development")
        {
            loggerConfiguration
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Orleans", LogEventLevel.Information)
                .MinimumLevel.Information();
        }

        Log.Logger = loggerConfiguration.CreateLogger();
    }
}
