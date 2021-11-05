// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MqttServiceConfiguration.cs" company="Hämmer Electronics">
//   Copyright (c) 2020 All rights reserved.
// </copyright>
// <summary>
//   The <see cref="MqttServiceConfiguration" /> read from the configuration file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleMqttServer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     The <see cref="MqttServiceConfiguration" /> read from the configuration file.
    /// </summary>
    public class MqttServiceConfiguration
    {
        /// <summary>
        ///     Gets or sets the port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///     Gets or sets the list of valid users.
        /// </summary>
        public List<User> Users { get; set; } = new ();

        /// <summary>
        /// Gets or sets the heartbeat delay in milliseconds.
        /// </summary>
        public int DelayInMilliSeconds { get; set; } = 30000;

        /// <summary>
        /// Checks whether the configuration is valid or not.
        /// </summary>
        /// <returns>A value indicating whether the configuration is valid or not.</returns>
        public bool IsValid()
        {
            if (this.Port is <= 0 or > 65535)
            {
                throw new Exception("The port is invalid");
            }

            if (!this.Users.Any())
            {
                throw new Exception("The users are invalid");
            }

            if (this.DelayInMilliSeconds <= 0)
            {
                throw new Exception("The heartbeat delay is invalid");
            }

            return true;
        }
    }
}