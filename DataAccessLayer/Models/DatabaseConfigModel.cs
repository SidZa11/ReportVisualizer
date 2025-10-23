using System;

namespace ReportVisualizer.DataAccessLayer.Models
{
    /// <summary>
    /// Model for database configuration settings
    /// </summary>
    public class DatabaseConfigModel
    {
        /// <summary>
        /// Gets or sets the server name
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the database name
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the authentication type (Windows/SQL)
        /// </summary>
        public string AuthenticationType { get; set; }

        /// <summary>
        /// Gets or sets the username (for SQL authentication)
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password (for SQL authentication)
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets whether to use integrated security
        /// </summary>
        public bool UseIntegratedSecurity { get; set; }

        /// <summary>
        /// Gets or sets the connection timeout in seconds
        /// </summary>
        public int ConnectionTimeout { get; set; } = 30;

        /// <summary>
        /// Gets or sets whether to trust the server certificate (for SSL issues)
        /// </summary>
        public bool TrustServerCertificate { get; set; }

        /// <summary>
        /// Gets or sets the date when the configuration was last modified
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>
        /// Generates a connection string based on the configuration
        /// </summary>
        /// <returns>SQL connection string</returns>
        public string GenerateConnectionString()
        {
            if (UseIntegratedSecurity)
            {
                return $"Server={ServerName};Database={DatabaseName};Integrated Security=True;Connect Timeout={ConnectionTimeout};TrustServerCertificate={TrustServerCertificate};";
            }
            else
            {
                return $"Server={ServerName};Database={DatabaseName};User Id={Username};Password={Password};Connect Timeout={ConnectionTimeout};TrustServerCertificate={TrustServerCertificate};";
            }
        }
    }
}