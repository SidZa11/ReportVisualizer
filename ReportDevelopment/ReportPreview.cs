using System;
using System.IO;
using ReportVisualizer.DataAccessLayer.Models;
using ReportVisualizer.Utilities;

namespace ReportVisualizer.ReportDevelopment
{
    /// <summary>
    /// Renders report preview
    /// </summary>
    public class ReportPreview
    {
        /// <summary>
        /// Generates a preview of the report
        /// </summary>
        /// <param name="reportData">Report data model</param>
        /// <param name="templateContent">Template content</param>
        /// <returns>Path to the preview file</returns>
        public string GeneratePreview(ReportDataModel reportData, string templateContent)
        {
            try
            {
                // In a real implementation, this would use the ReportViewer control
                // to render the report with the template and data
                // For this example, we'll just log the action
                
                Logger.Log($"Preview generated for report '{reportData.ReportName}' with {reportData.ReportData?.Rows.Count ?? 0} rows");
                
                // Return a mock preview path
                string previewPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Previews", $"{reportData.ReportId}.html");
                
                return previewPath;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error generating preview: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Exports the report preview to a file
        /// </summary>
        /// <param name="reportData">Report data model</param>
        /// <param name="format">Export format (PDF, Excel, etc.)</param>
        /// <returns>Path to the exported file</returns>
        public string ExportPreview(ReportDataModel reportData, string format)
        {
            try
            {
                // In a real implementation, this would export the report to the specified format
                // For this example, we'll just log the action
                
                Logger.Log($"Report '{reportData.ReportName}' exported to {format}");
                
                // Return a mock export path
                string exportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports", $"{reportData.ReportId}.{format.ToLower()}");
                
                return exportPath;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error exporting preview: {ex.Message}");
                throw;
            }
        }
    }
}