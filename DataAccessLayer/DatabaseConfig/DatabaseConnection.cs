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

        public bool IsConnected
        {
            get { return _connection != null && _connection.State == ConnectionState.Open; }
        }

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
                    // Connection should be initialized via GlobalConnectionHandler.Initialize()
                    // If it's null or closed here, it means initialization failed or hasn't happened.
                    // We should not attempt to re-initialize without a connection string.
                    throw new InvalidOperationException("Database connection is not initialized or is closed. Please ensure GlobalConnectionHandler.Initialize() has been called with a valid connection string.");
                }
                return _connection;
            }
        }

        /// <summary>
        /// Initializes the database connection using configuration settings
        /// </summary>
        public void InitializeConnection(string connectionString)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    Logger.LogError("Connection string is null or empty. Cannot initialize database connection.");
                    _connection = null;
                    return;
                }

                _connection = new SqlConnection(connectionString);
                _connection.Open();
                Logger.Log("Database connection established successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error initializing database connection: {ex.Message}");
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