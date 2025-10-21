using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ReportVisualizer.Utilities;
using ReportVisualizer.DataAccessLayer.Models;
using ConfigManager = ReportVisualizer.Utilities.ConfigurationManager;

namespace ReportVisualizer.DataAccessLayer.DatabaseConfig
{
    /// <summary>
    /// UI component for database and table selection
    /// </summary>
    public class DatabaseSelector
    {
        private readonly DatabaseConnection _dbConnection;

        public DatabaseSelector()
        {
            _dbConnection = DatabaseConnection.Instance;
        }

        /// <summary>
        /// Gets a list of available databases
        /// </summary>
        /// <returns>List of database names</returns>
        public List<string> GetAvailableDatabases()
        {
            List<string> databases = new List<string>();
            try
            {
                string query = "SELECT name FROM sys.databases WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb')";
                using (SqlCommand command = new SqlCommand(query, _dbConnection.Connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            databases.Add(reader["name"].ToString());
                        }
                    }
                }
                Logger.Log($"Retrieved {databases.Count} available databases");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error retrieving databases: {ex.Message}");
                throw;
            }
            return databases;
        }

        /// <summary>
        /// Gets a list of tables for the specified database
        /// </summary>
        /// <param name="databaseName">Name of the database</param>
        /// <returns>List of table names</returns>
        public List<string> GetTablesForDatabase(string databaseName)
        {
            List<string> tables = new List<string>();
            try
            {
                // Change database context
                _dbConnection.Connection.ChangeDatabase(databaseName);
                
                string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
                using (SqlCommand command = new SqlCommand(query, _dbConnection.Connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tables.Add(reader["TABLE_NAME"].ToString());
                        }
                    }
                }
                Logger.Log($"Retrieved {tables.Count} tables from database '{databaseName}'");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error retrieving tables for database '{databaseName}': {ex.Message}");
                throw;
            }
            return tables;
        }

        /// <summary>
        /// Gets the schema for a specified table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <returns>DataTable containing the schema</returns>
        public DataTable GetTableSchema(string tableName)
        {
            DataTable schema = new DataTable();
            try
            {
                string query = $"SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";
                using (SqlCommand command = new SqlCommand(query, _dbConnection.Connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(schema);
                    }
                }
                Logger.Log($"Retrieved schema for table '{tableName}' with {schema.Rows.Count} columns");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error retrieving schema for table '{tableName}': {ex.Message}");
                throw;
            }
            return schema;
        }

        /// <summary>
        /// Saves the database configuration
        /// </summary>
        /// <param name="config">Database configuration model</param>
        /// <returns>True if successful</returns>
        public bool SaveDatabaseConfig(DatabaseConfigModel config)
        {
            try
            {
                // Save configuration to application settings
                ConfigManager.SaveDatabaseConfig(config);
                Logger.Log($"Database configuration saved for database '{config.DatabaseName}'");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving database configuration: {ex.Message}");
                return false;
            }
        }
    }
}