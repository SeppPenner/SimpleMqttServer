// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MqttService.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
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
    /// The client identifiers of the currently connected clients. A concurrent collection because
    /// the MQTT server raises its connect and disconnect events from different threads.
    /// </summary>
    private readonly ConcurrentDictionary<string, byte> clientIds = new();

    /// <summary>
    /// The MQTT server. Null until the service is started and again after it is stopped.
    /// </summary>
    private MqttServer? mqttServer;

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
        this.serviceName = serviceName;

        // Create the logger.
        this.logger = LoggerConfig.GetLoggerConfiguration(nameof(MqttService))
            .WriteTo.Sink((ILogEventSink)Log.Logger)
            .CreateLogger();
    }

    /// <inheritdoc cref="BackgroundService"/>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!this.MqttServiceConfiguration.IsValid())
        {
            throw new Exception("The configuration is invalid");
        }

        this.logger.Information("Starting service");
        await this.StartMqttServerAsync();
        this.logger.Information("Service started");
        await base.StartAsync(cancellationToken);
    }

    /// <inheritdoc cref="BackgroundService"/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (this.mqttServer is not null)
        {
            await this.mqttServer.StopAsync();
            this.mqttServer.Dispose();
            this.mqttServer = null;
        }

        this.clientIds.Clear();
        this.logger.Information("Service stopped");
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
    /// Validates the MQTT connection.
    /// </summary>
    /// <param name="args">The arguments.</param>
    public Task ValidateConnectionAsync(ValidatingConnectionEventArgs args)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(args.UserName))
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return Task.CompletedTask;
            }

            var currentUser = this.MqttServiceConfiguration.Users.FirstOrDefault(u => u.UserName == args.UserName);

            if (currentUser is null)
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.LogMessage(args, true);
                return Task.CompletedTask;
            }

            if (args.UserName != currentUser.UserName)
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.LogMessage(args, true);
                return Task.CompletedTask;
            }

            if (args.Password != currentUser.Password)
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.LogMessage(args, true);
                return Task.CompletedTask;
            }

            // Registering the client identifier is the check for a duplicate one at the same time.
            // Doing both in one step keeps two clients with the same identifier from passing here
            // at once, and it only happens for a client that authenticated successfully.
            if (!this.clientIds.TryAdd(args.ClientId, 0))
            {
                args.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
                this.logger.Warning("A client with client id {ClientId} is already connected", args.ClientId);
                return Task.CompletedTask;
            }

            args.ReasonCode = MqttConnectReasonCode.Success;
            this.LogMessage(args, false);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            this.logger.Error("An error occurred: {Exception}.", ex);
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Validates the MQTT subscriptions.
    /// </summary>
    /// <param name="args">The arguments.</param>
    public Task InterceptSubscriptionAsync(InterceptingSubscriptionEventArgs args)
    {
        try
        {
            args.ProcessSubscription = true;
            this.LogMessage(args);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            this.logger.Error("An error occurred: {Exception}.", ex);
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Validates the MQTT application messages.
    /// </summary>
    /// <param name="args">The arguments.</param>
    public Task InterceptApplicationMessagePublishAsync(InterceptingPublishEventArgs args)
    {
        try
        {
            args.ProcessPublish = true;
            this.LogMessage(args);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            this.logger.Error("An error occurred: {Exception}.", ex);
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Handles the client disconnected event.
    /// </summary>
    /// <param name="args">The arguments.</param>
    private Task ClientDisconnectedAsync(ClientDisconnectedEventArgs args)
    {
        this.clientIds.TryRemove(args.ClientId, out _);
        this.logger.Information("Client disconnected: ClientId = {ClientId}", args.ClientId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts the MQTT server.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    private async Task StartMqttServerAsync()
    {
        var optionsBuilder = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(this.MqttServiceConfiguration.Port)
            .WithEncryptedEndpointPort(this.MqttServiceConfiguration.TlsPort);

        this.mqttServer = new MqttServerFactory().CreateMqttServer(optionsBuilder.Build());
        this.mqttServer.ValidatingConnectionAsync += this.ValidateConnectionAsync;
        this.mqttServer.InterceptingSubscriptionAsync += this.InterceptSubscriptionAsync;
        this.mqttServer.InterceptingPublishAsync += this.InterceptApplicationMessagePublishAsync;
        this.mqttServer.ClientDisconnectedAsync += this.ClientDisconnectedAsync;
        await this.mqttServer.StartAsync();
    }

    /// <summary> 
    ///     Logs the message from the MQTT subscription interceptor context. 
    /// </summary> 
    /// <param name="args">The arguments.</param>
    private void LogMessage(InterceptingSubscriptionEventArgs args)
    {
        this.logger.Information(
            "New subscription: ClientId = {ClientId}, TopicFilter = {TopicFilter}",
            args.ClientId,
            args.TopicFilter);
    }

    /// <summary>
    ///     Logs the message from the MQTT message interceptor context.
    /// </summary>
    /// <param name="args">The arguments.</param>
    private void LogMessage(InterceptingPublishEventArgs args)
    {
        var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);

        this.logger.Information(
            "Message: ClientId = {ClientId}, Topic = {Topic}, Payload = {Payload}, QoS = {Qos}, Retain-Flag = {RetainFlag}",
            args.ClientId,
            args.ApplicationMessage?.Topic,
            payload,
            args.ApplicationMessage?.QualityOfServiceLevel,
            args.ApplicationMessage?.Retain);
    }

    /// <summary> 
    ///     Logs the message from the MQTT connection validation context. 
    /// </summary> 
    /// <param name="args">The arguments.</param>
    /// <param name="showPassword">A <see cref="bool"/> value indicating whether the password is written to the log or not.</param>
    private void LogMessage(ValidatingConnectionEventArgs args, bool showPassword)
    {
        if (showPassword)
        {
            this.logger.Information(
                "New connection: ClientId = {ClientId}, Endpoint = {@Endpoint}, Username = {UserName}, Password = {Password}, CleanSession = {CleanSession}",
                args.ClientId,
                args.RemoteEndPoint,
                args.UserName,
                args.Password,
                args.CleanSession);
        }
        else
        {
            this.logger.Information(
                "New connection: ClientId = {ClientId}, Endpoint = {@Endpoint}, Username = {UserName}, CleanSession = {CleanSession}",
                args.ClientId,
                args.RemoteEndPoint,
                args.UserName,
                args.CleanSession);
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
