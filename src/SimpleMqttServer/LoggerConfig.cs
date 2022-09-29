// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggerConfig.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class that contains the main logger configuration.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleMqttServer;

/// <summary>
/// A class that contains the main logger configuration.
/// </summary>
public static class LoggerConfig
{
    /// <summary>
    /// Gets the logger configuration.
    /// </summary>
    /// <param name="type">The logger type.</param>
    /// <returns>The <see cref="LoggerConfiguration"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if the logger type is null.</exception>
    public static LoggerConfiguration GetLoggerConfiguration(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException(nameof(type), "The type of logger must be given.");
        }

        // set up logging for data frame output
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("LoggerType", type);
    }
}
