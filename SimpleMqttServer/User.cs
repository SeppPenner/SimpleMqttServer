namespace SimpleMqttServer
{
    /// <summary>
    ///     The <see cref="User" /> read from the config.json file.
    /// </summary>
    public class User
    {
        /// <summary>
        ///     Gets or sets the user name.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        ///     Gets or sets the password.
        /// </summary>
        public string Password { get; set; }
    }
}