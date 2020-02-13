using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json;
using Serilog;

namespace SimpleMqttServer
{
    /// <summary>
    ///     The main program.
    /// </summary>
    public class Program
    {
        /// <summary>
        ///     The main method that starts the service.
        /// </summary>
        /// <param name="args">Some arguments. Currently unused.</param>
        [SuppressMessage(
            "StyleCop.CSharp.DocumentationRules",
            "SA1650:ElementDocumentationMustBeSpelledCorrectly",
            Justification = "Reviewed. Suppression is OK here.")]
        public static void Main(string[] args)
        {
            var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(currentPath,
                    @"log\NetCoreMQTTExampleJsonConfig_.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var config = ReadConfiguration(currentPath);

            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint().WithDefaultEndpointPort(config.Port).WithConnectionValidator(
                    c =>
                    {
                        var currentUser = config.Users.FirstOrDefault(u => u.UserName == c.Username);

                        if (currentUser == null)
                        {
                            c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                            LogMessage(c, true);
                            return;
                        }

                        if (c.Username != currentUser.UserName)
                        {
                            c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                            LogMessage(c, true);
                            return;
                        }

                        if (c.Password != currentUser.Password)
                        {
                            c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                            LogMessage(c, true);
                            return;
                        }

                        c.ReasonCode = MqttConnectReasonCode.Success;
                        LogMessage(c, false);
                    }).WithSubscriptionInterceptor(
                    c =>
                    {
                        var userFound = c.SessionItems.TryGetValue(c.ClientId, out var currentUserObject);

                        if (!userFound || !(currentUserObject is User))
                        {
                            c.AcceptSubscription = false;
                            LogMessage(c, false);
                            return;
                        }

                        c.AcceptSubscription = true;
                        LogMessage(c, true);
                    }).WithApplicationMessageInterceptor(
                    c =>
                    {
                        var userFound = c.SessionItems.TryGetValue(c.ClientId, out var currentUserObject);

                        if (!userFound || !(currentUserObject is User))
                        {
                            c.AcceptPublish = false;
                            return;
                        }

                        c.AcceptPublish = true;
                    });

            var mqttServer = new MqttFactory().CreateMqttServer();
            mqttServer.StartAsync(optionsBuilder.Build());
            Console.ReadLine();
        }

        /// <summary>
        ///     Reads the configuration.
        /// </summary>
        /// <param name="currentPath">The current path.</param>
        /// <returns>A <see cref="Config" /> object.</returns>
        private static Config ReadConfiguration(string currentPath)
        {
            var filePath = $"{currentPath}\\config.json";

            Config config = null;

            // ReSharper disable once InvertIf
            if (File.Exists(filePath))
            {
                using var r = new StreamReader(filePath);
                var json = r.ReadToEnd();
                config = JsonConvert.DeserializeObject<Config>(json);
            }

            return config;
        }

        /// <summary> 
        ///     Logs the message from the MQTT subscription interceptor context. 
        /// </summary> 
        /// <param name="context">The MQTT subscription interceptor context.</param> 
        /// <param name="successful">A <see cref="bool"/> value indicating whether the subscription was successful or not.</param> 
        private static void LogMessage(MqttSubscriptionInterceptorContext context, bool successful)
        {
            Log.Information(successful ? $"New subscription: ClientId = {context.ClientId}, TopicFilter = {context.TopicFilter}" : $"Subscription failed for clientId = {context.ClientId}, TopicFilter = {context.TopicFilter}");
        }

        /// <summary> 
        ///     Logs the message from the MQTT connection validation context. 
        /// </summary> 
        /// <param name="context">The MQTT connection validation context.</param> 
        /// <param name="showPassword">A <see cref="bool"/> value indicating whether the password is written to the log or not.</param> 
        private static void LogMessage(MqttConnectionValidatorContext context, bool showPassword)
        {
            if (showPassword)
            {
                Log.Information(
                    $"New connection: ClientId = {context.ClientId}, Endpoint = {context.Endpoint},"
                    + $" Username = {context.Username}, Password = {context.Password},"
                    + $" CleanSession = {context.CleanSession}");
            }
            else
            {
                Log.Information(
                    $"New connection: ClientId = {context.ClientId}, Endpoint = {context.Endpoint},"
                    + $" Username = {context.Username}, CleanSession = {context.CleanSession}");
            }
        }
    }
}