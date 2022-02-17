// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MqttService.cs" company="Hämmer Electronics">
//   Copyright (c) 2020 All rights reserved.
// </copyright>
// <summary>
//   The main service class of the <see cref="MqttService" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleMqttServer;

/// <inheritdoc cref="BackgroundService"/>
/// <summary>
///     The main service class of the <see cref="MqttService" />.
/// </summary>
public class MqttService : BackgroundService
{
    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger logger;

    /// <summary>
    /// The service name.
    /// </summary>
    private readonly string serviceName;

    /// <summary>
    /// The bytes divider. (Used to convert from bytes to kilobytes and so on).
    /// </summary>
    private static double BytesDivider => 1048576.0;

    /// <summary>
    /// Gets or sets the MQTT service configuration.
    /// </summary>
    public MqttServiceConfiguration MqttServiceConfiguration { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttService"/> class.
    /// </summary>
    /// <param name="mqttServiceConfiguration">The MQTT service configuration.</param>
    /// <param name="serviceName">The service name.</param>
    public MqttService(MqttServiceConfiguration mqttServiceConfiguration, string serviceName)
    {
        this.MqttServiceConfiguration = mqttServiceConfiguration;
        this.logger = Log.ForContext("Type", nameof(MqttService));
        this.serviceName = serviceName;
    }

    /// <inheritdoc cref="BackgroundService"/>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!this.MqttServiceConfiguration.IsValid())
        {
            throw new Exception("The configuration is invalid");
        }

        this.logger.Information("Starting service");
        this.StartMqttServer();
        this.logger.Information("Service started");
        await base.StartAsync(cancellationToken);
    }

    /// <inheritdoc cref="BackgroundService"/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
    }

    /// <inheritdoc cref="BackgroundService"/>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Log some memory information.
                this.LogMemoryInformation();
                await Task.Delay(this.MqttServiceConfiguration.DelayInMilliSeconds, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.Error("An error occurred: {Exception}", ex);
            }
        }
    }

    /// <summary>
    /// Starts the MQTT server.
    /// </summary>
    private void StartMqttServer()
    {
        var optionsBuilder = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(this.MqttServiceConfiguration.Port)
            .WithEncryptedEndpointPort(this.MqttServiceConfiguration.TlsPort)
            .WithConnectionValidator(
                c =>
                {
                    var currentUser = this.MqttServiceConfiguration.Users.FirstOrDefault(u => u.UserName == c.Username);

                    if (currentUser == null)
                    {
                        c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                        this.LogMessage(c, true);
                        return;
                    }

                    if (c.Username != currentUser.UserName)
                    {
                        c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                        this.LogMessage(c, true);
                        return;
                    }

                    if (c.Password != currentUser.Password)
                    {
                        c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                        this.LogMessage(c, true);
                        return;
                    }

                    c.ReasonCode = MqttConnectReasonCode.Success;
                    this.LogMessage(c, false);
                }).WithSubscriptionInterceptor(
                c =>
                {
                    c.AcceptSubscription = true;
                    this.LogMessage(c, true);
                }).WithApplicationMessageInterceptor(
                c =>
                {
                    c.AcceptPublish = true;
                    this.LogMessage(c);
                });

        var mqttServer = new MqttFactory().CreateMqttServer();
        mqttServer.StartAsync(optionsBuilder.Build());
    }

    /// <summary> 
    ///     Logs the message from the MQTT subscription interceptor context. 
    /// </summary> 
    /// <param name="context">The MQTT subscription interceptor context.</param> 
    /// <param name="successful">A <see cref="bool"/> value indicating whether the subscription was successful or not.</param> 
    private void LogMessage(MqttSubscriptionInterceptorContext context, bool successful)
    {
        this.logger.Information(
            successful
                ? "New subscription: ClientId = {clientId}, TopicFilter = {topicFilter}"
                : "Subscription failed for clientId = {clientId}, TopicFilter = {topicFilter}",
            context.ClientId,
            context.TopicFilter);
    }

    /// <summary>
    ///     Logs the message from the MQTT message interceptor context.
    /// </summary>
    /// <param name="context">The MQTT message interceptor context.</param>
    private void LogMessage(MqttApplicationMessageInterceptorContext context)
    {
        var payload = context.ApplicationMessage?.Payload == null ? null : Encoding.UTF8.GetString(context.ApplicationMessage.Payload);

        this.logger.Information(
            "Message: ClientId = {clientId}, Topic = {topic}, Payload = {payload}, QoS = {qos}, Retain-Flag = {retainFlag}",
            context.ClientId,
            context.ApplicationMessage?.Topic,
            payload,
            context.ApplicationMessage?.QualityOfServiceLevel,
            context.ApplicationMessage?.Retain);
    }

    /// <summary> 
    ///     Logs the message from the MQTT connection validation context. 
    /// </summary> 
    /// <param name="context">The MQTT connection validation context.</param> 
    /// <param name="showPassword">A <see cref="bool"/> value indicating whether the password is written to the log or not.</param> 
    private void LogMessage(MqttConnectionValidatorContext context, bool showPassword)
    {
        if (showPassword)
        {
            this.logger.Information(
                "New connection: ClientId = {clientId}, Endpoint = {endpoint}, Username = {userName}, Password = {password}, CleanSession = {cleanSession}",
                context.ClientId,
                context.Endpoint,
                context.Username,
                context.Password,
                context.CleanSession);
        }
        else
        {
            this.logger.Information(
                "New connection: ClientId = {clientId}, Endpoint = {endpoint}, Username = {userName}, CleanSession = {cleanSession}",
                context.ClientId,
                context.Endpoint,
                context.Username,
                context.CleanSession);
        }
    }

    /// <summary>
    /// Logs the heartbeat message with some memory information.
    /// </summary>
    private void LogMemoryInformation()
    {
        var totalMemory = GC.GetTotalMemory(false);
        var memoryInfo = GC.GetGCMemoryInfo();
        var divider = BytesDivider;
        Log.Information(
            "Heartbeat for service {ServiceName}: Total {Total}, heap size: {HeapSize}, memory load: {MemoryLoad}.",
            this.serviceName, $"{(totalMemory / divider):N3}", $"{(memoryInfo.HeapSizeBytes / divider):N3}", $"{(memoryInfo.MemoryLoadBytes / divider):N3}");
    }
}
