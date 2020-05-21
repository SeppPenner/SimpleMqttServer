// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Config.cs" company="Haemmer Electronics">
//   Copyright (c) 2020 All rights reserved.
// </copyright>
// <summary>
//   The <see cref="Config" /> read from the config.json file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleMqttServer
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///     The <see cref="Config" /> read from the config.json file.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
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