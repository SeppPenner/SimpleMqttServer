// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Startup.cs" company="Hämmer Electronics">
//   Copyright (c) 2020 All rights reserved.
// </copyright>
// <summary>
//   The startup class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleMqttServer;

/// <summary>
/// The startup class.
/// </summary>
public class Startup
{
    /// <summary>
    /// The service name.
    /// </summary>
    private readonly AssemblyName serviceName = Assembly.GetExecutingAssembly().GetName();

    /// <summary>
    /// Gets the MQTT service configuration.
    /// </summary>
    private readonly MqttServiceConfiguration mqttServiceConfiguration = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Startup"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public Startup(IConfiguration configuration)
    {
        configuration.GetSection(this.serviceName.Name).Bind(this.mqttServiceConfiguration);
    }

    /// <summary>
    /// Configures the services.
    /// </summary>
    /// <param name="services">The services.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions();

        services.AddSingleton(this.mqttServiceConfiguration);

        services.AddMvc().AddRazorPagesOptions(options => { options.RootDirectory = "/"; })
            .AddDataAnnotationsLocalization();

        // Workaround to have a hosted background service available by DI.
        services.AddSingleton(_ => new MqttService(this.mqttServiceConfiguration, this.serviceName.Name ?? "MqttService"));
        services.AddSingleton<IHostedService>(p => p.GetRequiredService<MqttService>());
    }

    /// <summary>
    /// This method gets called by the runtime.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="env">The web hosting environment.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSerilogRequestLogging();
        app.UseRouting();

        _ = app.ApplicationServices.GetService<MqttService>();
    }
}
