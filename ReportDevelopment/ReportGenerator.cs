using System;
using System.Data;
using Microsoft.Data.SqlClient;
using ReportVisualizer.DataAccessLayer.Models;
using ReportVisualizer.Utilities;

namespace ReportVisualizer.ReportDevelopment
{
    /// <summary>
    /// Handles report generation logic
    /// </summary>
    public class ReportGenerator
    {
        /// <summary>
        /// Generates a report based on the provided configuration
        /// </summary>
        /// <param name="reportData">Report data model</param>
        /// <returns>Generated report data</returns>
        public ReportDataModel GenerateReport(ReportDataModel reportData)
        {
            try
            {
                // Validate input
                if (reportData == null)
                {
                    throw new ArgumentNullException(nameof(reportData));
                }

                if (string.IsNullOrEmpty(reportData.SqlQuery))
                {
                    throw new ArgumentException("SQL query cannot be empty");
                }

                // Execute query to get report data
                reportData.ReportData = ExecuteQuery(reportData.DatabaseConfig, reportData.SqlQuery);
                
                // Set creation metadata
                reportData.CreatedDate = DateTime.Now;
                reportData.LastModifiedDate = DateTime.Now;
                
                Logger.Log($"Report '{reportData.ReportName}' generated successfully with {reportData.ReportData.Rows.Count} rows");
                
                return reportData;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error generating report: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Executes a SQL query and returns the results
        /// </summary>
        /// <param name="dbConfig">Database configuration</param>
        /// <param name="sqlQuery">SQL query to execute</param>
        /// <returns>DataTable with query results</returns>
        private DataTable ExecuteQuery(DatabaseConfigModel dbConfig, string sqlQuery)
        {
            DataTable dataTable = new DataTable();
            
            try
            {
                string connectionString = dbConfig.GenerateConnectionString();
                
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        // Set command timeout
                        command.CommandTimeout = 120; // 2 minutes
                        
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                }
                
                Logger.Log($"Query executed successfully, returned {dataTable.Rows.Count} rows");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error executing query: {ex.Message}");
                throw;
            }
            
            return dataTable;
        }
    }
}