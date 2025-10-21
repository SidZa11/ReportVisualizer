using System;
using System.IO;
using ReportVisualizer.DataAccessLayer.Models;
using ReportVisualizer.Utilities;

namespace ReportVisualizer.ReportViewer
{
    /// <summary>
    /// Handles final report display
    /// </summary>
    public class ReportRenderer
    {
        /// <summary>
        /// Renders a report for display
        /// </summary>
        /// <param name="report">Report data model</param>
        /// <returns>HTML content for display</returns>
        public string RenderReport(ReportDataModel report)
        {
            try
            {
                // In a real implementation, this would use the ReportViewer control
                // to render the report with the template and data
                // For this example, we'll just log the action and return mock HTML
                
                Logger.Log($"Rendered report '{report.ReportName}' for display");
                
                // Generate mock HTML content
                string html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>{report.ReportName}</title>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <style>
                        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; }}
                        .report-header {{ background-color: #f0f0f0; padding: 10px; margin-bottom: 20px; }}
                        .report-title {{ font-size: 24px; font-weight: bold; }}
                        .report-description {{ font-size: 14px; color: #666; }}
                        .report-data {{ width: 100%; border-collapse: collapse; }}
                        .report-data th {{ background-color: #4CAF50; color: white; text-align: left; padding: 8px; }}
                        .report-data td {{ border: 1px solid #ddd; padding: 8px; }}
                        .report-data tr:nth-child(even) {{ background-color: #f2f2f2; }}
                        @media screen and (max-width: 600px) {{
                            .report-data {{ font-size: 12px; }}
                            .report-data th, .report-data td {{ padding: 4px; }}
                        }}
                    </style>
                </head>
                <body>
                    <div class='report-header'>
                        <div class='report-title'>{report.ReportName}</div>
                        <div class='report-description'>{report.Description}</div>
                    </div>
                    <div class='report-content'>
                        <!-- Report content would be rendered here -->
                    </div>
                </body>
                </html>";
                
                return html;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error rendering report: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Exports the report to a file
        /// </summary>
        /// <param name="report">Report data model</param>
        /// <param name="format">Export format (PDF, Excel, etc.)</param>
        /// <returns>Path to the exported file</returns>
        public string ExportReport(ReportDataModel report, string format)
        {
            try
            {
                // In a real implementation, this would export the report to the specified format
                // For this example, we'll just log the action
                
                Logger.Log($"Report '{report.ReportName}' exported to {format}");
                
                // Return a mock export path
                string exportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports", $"{report.ReportId}.{format.ToLower()}");
                
                return exportPath;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error exporting report: {ex.Message}");
                throw;
            }
        }
    }
}