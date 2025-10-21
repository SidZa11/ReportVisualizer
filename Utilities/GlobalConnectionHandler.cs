using System;
using ReportVisualizer.DataAccessLayer.DatabaseConfig;

namespace ReportVisualizer.Utilities
{
    /// <summary>
    /// Maintains a single client connection throughout the application lifecycle
    /// </summary>
    public static class GlobalConnectionHandler
    {
        private static readonly DatabaseConnection _databaseConnection = DatabaseConnection.Instance;
        private static bool _isInitialized = false;

        /// <summary>
        /// Initializes the global connection handler
        /// </summary>
        public static void Initialize()
        {
            if (!_isInitialized)
            {
                try
                {
                    // Access the connection to initialize it
                    var connection = _databaseConnection.Connection;
                    _isInitialized = true;
                    Logger.Log("Global connection handler initialized");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error initializing global connection handler: {ex.Message}");
                    // Don't throw the exception, just log it to allow the application to start
                }
            }
        }

        /// <summary>
        /// Gets the database connection instance
        /// </summary>
        /// <returns>DatabaseConnection instance</returns>
        public static DatabaseConnection GetDatabaseConnection()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            return _databaseConnection;
        }

        /// <summary>
        /// Closes the database connection
        /// </summary>
        public static void CloseConnection()
        {
            if (_isInitialized)
            {
                _databaseConnection.CloseConnection();
                _isInitialized = false;
                Logger.Log("Global connection closed");
            }
        }
    }
}