using System;
using System.Data;
using Microsoft.Data.SqlClient;
using ReportVisualizer.Utilities;
// Use fully qualified name to avoid ambiguity
using ConfigManager = ReportVisualizer.Utilities.ConfigurationManager;

namespace ReportVisualizer.DataAccessLayer.DatabaseConfig
{
    /// <summary>
    /// Handles database connection management using singleton pattern
    /// </summary>
    public class DatabaseConnection
    {
        private static DatabaseConnection _instance;
        private static readonly object _lock = new object();
        private SqlConnection _connection;

        private DatabaseConnection()
        {
            // Private constructor to enforce singleton pattern
        }

        /// <summary>
        /// Gets the singleton instance of DatabaseConnection
        /// </summary>
        public static DatabaseConnection Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DatabaseConnection();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets the current SqlConnection
        /// </summary>
        public SqlConnection Connection
        {
            get
            {
                if (_connection == null || _connection.State == ConnectionState.Closed)
                {
                    InitializeConnection();
                }
                return _connection;
            }
        }

        /// <summary>
        /// Initializes the database connection using configuration settings
        /// </summary>
        private void InitializeConnection()
        {
            try
            {
                string connectionString = ConfigManager.GetConnectionString();
                _connection = new SqlConnection(connectionString);
                _connection.Open();
                Logger.Log("Database connection established successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error initializing database connection: {ex.Message}");
                // Return null instead of throwing to allow application to start
                _connection = null;
            }
        }

        /// <summary>
        /// Closes the database connection
        /// </summary>
        public void CloseConnection()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                _connection.Close();
                Logger.Log("Database connection closed");
            }
        }
    }
}