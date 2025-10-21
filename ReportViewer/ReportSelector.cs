using System;
using System.Collections.Generic;
using ReportVisualizer.DataAccessLayer.Models;
using ReportVisualizer.Utilities;

namespace ReportVisualizer.ReportViewer
{
    /// <summary>
    /// UI component for report selection
    /// </summary>
    public class ReportSelector
    {
        // In a real application, this would be stored in a database
        private readonly List<ReportDataModel> _availableReports = new List<ReportDataModel>();
        
        /// <summary>
        /// Gets a list of available reports
        /// </summary>
        /// <returns>List of report data models</returns>
        public List<ReportDataModel> GetAvailableReports()
        {
            try
            {
                // In a real application, this would retrieve reports from a database
                Logger.Log($"Retrieved {_availableReports.Count} available reports");
                return _availableReports;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error retrieving available reports: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Adds a report to the available reports list
        /// </summary>
        /// <param name="report">Report data model</param>
        public void AddReport(ReportDataModel report)
        {
            try
            {
                _availableReports.Add(report);
                Logger.Log($"Added report '{report.ReportName}' to available reports");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error adding report: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets a report by ID
        /// </summary>
        /// <param name="reportId">Report ID</param>
        /// <returns>Report data model</returns>
        public ReportDataModel GetReportById(Guid reportId)
        {
            try
            {
                ReportDataModel report = _availableReports.Find(r => r.ReportId == reportId);
                
                if (report == null)
                {
                    throw new ArgumentException($"Report with ID '{reportId}' not found");
                }
                
                Logger.Log($"Retrieved report '{report.ReportName}'");
                return report;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error retrieving report: {ex.Message}");
                throw;
            }
        }
    }
}