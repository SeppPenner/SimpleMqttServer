// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MqttServiceConfiguration.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   The <see cref="MqttServiceConfiguration" /> read from the configuration file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleMqttServer;

/// <summary>
///     The <see cref="MqttServiceConfiguration" /> read from the configuration file.
/// </summary>
public class MqttServiceConfiguration
{
    /// <summary>
    ///     Gets or sets the port.
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    ///     Gets or sets the list of valid users.
    /// </summary>
    public List<User> Users { get; set; } = [];

    /// <summary>
    /// Gets or sets the heartbeat delay in milliseconds.
    /// </summary>
    public int DelayInMilliSeconds { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the TLS port.
    /// </summary>
    public int TlsPort { get; set; } = 8883;

    /// <summary>
    /// Gets or sets the path to the certificate used for the encrypted endpoint. As long as this is
    /// empty the encrypted endpoint stays off and the server listens on <see cref="Port"/> only.
    /// </summary>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password of the PKCS#12 certificate file. Ignored when
    /// <see cref="CertificateKeyPath"/> is set, a PEM key file carries no password here.
    /// </summary>
    public string CertificatePassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the private key belonging to <see cref="CertificatePath"/>. Setting
    /// it switches the certificate loading from PKCS#12 to the PEM format.
    /// </summary>
    public string CertificateKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the encrypted endpoint is configured or not. A configured
    /// certificate is the switch, there is no separate flag for it.
    /// </summary>
    public bool UseTls => !string.IsNullOrWhiteSpace(this.CertificatePath);

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

        if (this.TlsPort is <= 0 or > 65535)
        {
            throw new Exception("The TLS port is invalid");
        }

        if (this.UseTls)
        {
            if (this.Port == this.TlsPort)
            {
                throw new Exception("The port and the TLS port must not be the same");
            }

            if (!File.Exists(this.CertificatePath))
            {
                throw new Exception($"The certificate file {this.CertificatePath} does not exist");
            }

            if (!string.IsNullOrWhiteSpace(this.CertificateKeyPath) && !File.Exists(this.CertificateKeyPath))
            {
                throw new Exception($"The certificate key file {this.CertificateKeyPath} does not exist");
            }
        }

        return true;
    }
}
