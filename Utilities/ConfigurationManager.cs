using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using ReportVisualizer.DataAccessLayer.Models;

namespace ReportVisualizer.Utilities
{
    /// <summary>
    /// Manages application configuration and environment variables
    /// </summary>
    public static class ConfigurationManager
    {
        private static IConfiguration _configuration;

        /// <summary>
        /// Initializes the configuration manager
        /// </summary>
        /// <param name="configuration">IConfiguration instance</param>
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
            Logger.Log("Configuration manager initialized");
        }

        /// <summary>
        /// Gets a connection string from environment variables
        /// </summary>
        /// <returns>Connection string</returns>
        public static string GetConnectionString()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                Logger.LogWarning("Connection string not found in configuration");
                throw new InvalidOperationException("Connection string not found in configuration");
            }
            return connectionString;
        }

        /// <summary>
        /// Gets an application setting by key
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <returns>Setting value</returns>
        public static string GetAppSetting(string key)
        {
            string value = _configuration[key];
            if (string.IsNullOrEmpty(value))
            {
                Logger.LogWarning($"Application setting '{key}' not found");
            }
            return value;
        }

        /// <summary>
        /// Saves database configuration
        /// </summary>
        /// <param name="config">Database configuration model</param>
        public static void SaveDatabaseConfig(DatabaseConfigModel config)
        {
            try
            {
                // In a real application, this would save to a secure storage
                // For this example, we'll just log the action
                Logger.Log($"Database configuration saved: Server={config.ServerName}, Database={config.DatabaseName}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving database configuration: {ex.Message}");
                throw;
            }
        }
    }
}