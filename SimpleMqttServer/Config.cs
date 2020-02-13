using System.Collections.Generic;

namespace SimpleMqttServer
{
    /// <summary>
    ///     The <see cref="Config" /> read from the config.json file.
    /// </summary>
    public class Config
    {
        /// <summary>
        ///     Gets or sets the port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///     Gets or sets the list of valid users.
        /// </summary>
        public List<User> Users { get; set; } = new List<User>();
    }
}