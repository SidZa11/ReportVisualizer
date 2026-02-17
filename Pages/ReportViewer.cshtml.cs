using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;

using System.Collections.Generic;
using System.Text; // Added for Encoding
using System.Diagnostics;
using System; // Added for Exception
using Microsoft.Extensions.Configuration; // Add this using statement
using System.Data;
using System.IO;

using Microsoft.Reporting.NETCore;

using ReportVisualizer.ReportViewer;
using ReportVisualizer.ReportViewer.ReportDataExtraction; // Add this using statement
using ReportVisualizer.ReportViewer.ReportDataExecution; // Add this using statement
using ReportVisualizer.Utilities; // Add this using statement

using Microsoft.AspNetCore.Http;

namespace ReportVisualizer.Pages
{
    public class ReportViewerModel : PageModel
    {
        private readonly string _reportsPath = Path.Combine(Directory.GetCurrentDirectory(), "ReportViewer", "Reports");
        private readonly RdlDataExtractor _rdlDataExtractor;
        private readonly SqlDatasetExecutor _sqlDatasetExecutor;
        private readonly IConfiguration _configuration;

        public string ReportName { get; set; }
        public string ErrorMessage { get; set; }
        public string ReportHtmlContent { get; set; }
        public List<string> AvailableReports { get; set; }
        public List<QueryParameterInfo> QueryParameters { get; set; }
        public bool ShowParameterPopup { get; set; } = false;
        [BindProperty]
        public Dictionary<string, object> SubmittedParameters { get; set; }

        public ReportViewerModel(RdlDataExtractor rdlDataExtractor, SqlDatasetExecutor sqlDatasetExecutor, IConfiguration configuration)
        {
            _rdlDataExtractor = rdlDataExtractor;
            _sqlDatasetExecutor = sqlDatasetExecutor;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGet(string reportName)
        {
            var reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ReportViewer", "Reports");
            AvailableReports = Directory.GetFiles(reportsDirectory, "*.rdl")
                                        .Select(Path.GetFileNameWithoutExtension)
                                        .ToList();

            if (string.IsNullOrEmpty(reportName))
            {
                // If no report is selected, just show the list
                return Page();
            }

            ReportName = reportName;
            string reportFilePath = Path.Combine(_reportsPath, reportName + ".rdl");

            if (!System.IO.File.Exists(reportFilePath))
            {
                ErrorMessage = $"Report '{reportName}' not found.";
                return Page();
            }

            try
            {
                // Extract query parameters
                QueryParameters = _rdlDataExtractor.ExtractQueryParameters(reportFilePath);

                if (QueryParameters.Any())
                {
                    ShowParameterPopup = true;
                    // If there are query parameters, we need to show a popup to get their values.
                    // The report will not be loaded immediately.
                    return Page(); // Return Page to allow user to input parameters
                }

                // If no query parameters, proceed to render the report
                var reportRenderer = new ReportRenderer(_rdlDataExtractor, _sqlDatasetExecutor, _configuration);
                ReportHtmlContent = await reportRenderer.RenderReport(reportFilePath, new Dictionary<string, object>());
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error rendering report: {ex.Message}";
                Console.WriteLine($"Error rendering report: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSubmitParameters(string reportName, IFormCollection form)
        {
            var reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ReportViewer", "Reports");
            AvailableReports = Directory.GetFiles(reportsDirectory, "*.rdl")
                                        .Select(Path.GetFileNameWithoutExtension)
                                        .ToList();

            ReportName = reportName;
            Console.WriteLine($"ReportName: {ReportName}");
            string reportFilePath = Path.Combine(_reportsPath, reportName + ".rdl");

            if (!System.IO.File.Exists(reportFilePath))
            {
                ErrorMessage = $"Report '{reportName}' not found.";
                return Page();
            }

            try
            {
                var reportRenderer = new ReportRenderer(_rdlDataExtractor, _sqlDatasetExecutor, _configuration);
                var parametersAsObject = form
                    .Where(k => k.Key.StartsWith("SubmittedParameters["))
                    .ToDictionary(
                        k => k.Key.Replace("SubmittedParameters[", "").Replace("]", ""),
                        k => (object)k.Value.ToString()
                );

                ReportHtmlContent = await reportRenderer.RenderReport(reportFilePath, parametersAsObject);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(ex, $"Error rendering report with parameters: {reportName}");
                ErrorMessage = $"Error rendering report: {ex.Message}";
            }

            return Page();
        }

        public IActionResult OnGetEditPath(string reportName)
        {
            if (string.IsNullOrEmpty(reportName))
                return Content("ERROR: Report not selected");

            var reportPath = Path.Combine(_reportsPath, reportName + ".rdl");

            if (!System.IO.File.Exists(reportPath))
                return Content("ERROR: Report not found");

            // Launch Report Builder process
            LaunchReportBuilderProcess(reportPath);

            // Must return FULL WINDOWS PATH
            return Content("Opening Report Builder...");
        }

        public async Task<IActionResult> OnGetExport(string reportName, string format)
        {
            if (string.IsNullOrEmpty(reportName))
                return BadRequest("Report not selected");

            var reportPath = Path.Combine(_reportsPath, reportName + ".rdl");

            if (!System.IO.File.Exists(reportPath))
                return BadRequest("Report not found");

            LocalReport report = new();
            report.ReportPath = reportPath;

            var reportParameters = new Dictionary<string, object>();
            foreach (var queryParam in Request.Query)
            {
                if (queryParam.Key != "reportName" && queryParam.Key != "format")
                {
                    reportParameters.Add(queryParam.Key, queryParam.Value.ToString());
                }
            }

            await LoadReportDataAsync(report, reportPath, reportParameters);

            string renderFormat = format.ToLower() switch
            {
                "pdf" => "PDF",
                "excel" => "EXCELOPENXML",
                "word" => "WORDOPENXML",
                _ => "PDF"
            };

            string mimeType, encoding, extension;
            Warning[] warnings;
            string[] streamids;

            var bytes = report.Render(
                renderFormat, null,
                out mimeType, out encoding,
                out extension, out streamids, out warnings);

            return File(bytes, mimeType, $"{reportName}.{extension}");
        }


        private async Task LoadReportDataAsync(LocalReport report, string reportPath, Dictionary<string, object> parameters)
        {
            try
            {
                // Extract datasets from RDL
                var datasets = _rdlDataExtractor.ExtractDataSets(reportPath);
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                foreach (var dataset in datasets)
                {
                    // Execute dataset query
                    DataTable dt = await _sqlDatasetExecutor.ExecuteQueryAsync(
                        connectionString,
                        dataset.CommandText,
                        dataset.CommandType,
                        parameters
                    );

                    // IMPORTANT: dataset name must match RDL dataset name
                    report.DataSources.Add(new ReportDataSource(dataset.Name, dt));
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(ex, $"Error loading report data for report path: {reportPath}");
                throw; // Re-throw the exception after logging
            }
        }


        private void LaunchReportBuilderProcess(string rdlcFilePath)
        {
            string reportBuilderPath = _configuration["ReportBuilder:Path"];
            if (string.IsNullOrEmpty(reportBuilderPath))
            {
                Console.WriteLine("Report Builder path is not configured in appsettings.json.");
                return;
            }

            if (!System.IO.File.Exists(rdlcFilePath))
            {
                Console.WriteLine($"Report file '{rdlcFilePath}' not found for launching Report Builder.");
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = rdlcFilePath,
                    UseShellExecute = true,
                };

                Process.Start(psi);
                Console.WriteLine($"Report Builder launched for: {rdlcFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error launching Report Builder: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }


    }
}