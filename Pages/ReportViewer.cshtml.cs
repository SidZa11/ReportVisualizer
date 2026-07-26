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
using ReportVisualizer.Security;

namespace ReportVisualizer.Pages
{
    [RequireScadaLogin]
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
            // Ensure directory exists
            if (!Directory.Exists(reportsDirectory))
            {
                AvailableReports = new List<string>();
                ErrorMessage = "No reports available.";
                return Page();
            }
            AvailableReports = Directory.GetFiles(reportsDirectory, "*.rdl")
                                        .Select(Path.GetFileNameWithoutExtension)
                                        .ToList();
            // If folder exists but empty
            if (!AvailableReports.Any())
            {
                ErrorMessage = "No reports available.";
                return Page();
            }



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
                var rendered = await reportRenderer.RenderReportWithSnapshot(reportFilePath, new Dictionary<string, object>());
                ReportHtmlContent = rendered.Html;
                if (rendered.Snapshot != null)
                {
                    rendered.Snapshot.ReportName = reportName;
                    ReportSnapshotSessionStore.Store(HttpContext.Session, rendered.Snapshot);
                }
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
                HttpContext.Session.SetString(
                    "LastReportParameters",
                    System.Text.Json.JsonSerializer.Serialize(parametersAsObject)
                );
                var rendered = await reportRenderer.RenderReportWithSnapshot(reportFilePath, parametersAsObject);
                ReportHtmlContent = rendered.Html;
                if (rendered.Snapshot != null)
                {
                    rendered.Snapshot.ReportName = reportName;
                    ReportSnapshotSessionStore.Store(HttpContext.Session, rendered.Snapshot);
                }
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

            string renderFormat = (format ?? string.Empty).ToLower() switch
            {
                "pdf" => "PDF",
                "excel" => "EXCELOPENXML",
                "word" => "WORDOPENXML",
                _ => "PDF"
            };

            var preprocessOptions = new RdlPreprocessOptions
            {
                ForceRepeatHeaderRowsOnEveryPage = true,
                PromotePageHeaderToBodyForExcel = string.Equals(renderFormat, "EXCELOPENXML", StringComparison.OrdinalIgnoreCase),
                PromotePageFooterToBodyForExcel = string.Equals(renderFormat, "EXCELOPENXML", StringComparison.OrdinalIgnoreCase)
            };

            using var definitionStream = RdlPreprocessor.PreprocessFile(reportPath, preprocessOptions);
            using var report = new LocalReport();
            report.LoadReportDefinition(definitionStream);

            Console.WriteLine($"OnGetExport called for report: {reportName}, format: {format} -> {renderFormat}");

            var snapshot = ReportSnapshotSessionStore.Get(HttpContext.Session);
            bool usedCachedSnapshot = snapshot != null &&
                                      string.Equals(snapshot.ReportName, reportName, StringComparison.OrdinalIgnoreCase) &&
                                      snapshot.DataSources != null &&
                                      snapshot.DataSources.Count > 0;

            Dictionary<string, object> reportParameters;
            if (usedCachedSnapshot)
            {
                reportParameters = DataTableDtoMapper.NormalizeParameterDictionary(snapshot.Parameters ?? new Dictionary<string, object>());
                foreach (var ds in snapshot.DataSources)
                {
                    var dt = DataTableDtoMapper.ToDataTable(ds.Table);
                    if (string.IsNullOrWhiteSpace(dt.TableName)) dt.TableName = ds.Name;
                    report.DataSources.Add(new ReportDataSource(ds.Name, dt));
                }

                if (reportParameters.Count > 0)
                {
                    var rdlParams = reportParameters.Select(p =>
                    {
                        if (p.Value is Microsoft.Extensions.Primitives.StringValues sv)
                            return new ReportParameter(p.Key, sv.ToArray());
                        if (p.Value == null)
                            return new ReportParameter(p.Key, new[] { (string)null });
                        if (p.Value is IEnumerable<string> en)
                            return new ReportParameter(p.Key, new List<string>(en).ToArray());
                        return new ReportParameter(p.Key, Convert.ToString(p.Value));
                    }).ToList();
                    try { report.SetParameters(rdlParams); } catch { }
                }
            }
            else
            {
                var json = HttpContext.Session.GetString("LastReportParameters");

                if (string.IsNullOrEmpty(json))
                {
                    var rdlDefinedParameters = _rdlDataExtractor.ExtractQueryParameters(reportPath);
                    if (rdlDefinedParameters == null || rdlDefinedParameters.Count == 0)
                    {
                        reportParameters = new Dictionary<string, object>();
                    }
                    else
                    {
                        return BadRequest("No report parameters found. Preview report first.");
                    }
                }
                else
                {
                    reportParameters = System.Text.Json.JsonSerializer
                        .Deserialize<Dictionary<string, object>>(json);
                }

                Console.WriteLine("Report Parameters being passed to LoadReportDataAsync:");
                foreach (var param in reportParameters)
                {
                    Console.WriteLine($"- {param.Key}: {param.Value}");
                }

                await LoadReportDataAsync(report, reportPath, reportParameters);
            }

            string deviceInfo = BuildDeviceInfo(renderFormat);

            string mimeType, encoding, extension;
            Warning[] warnings;
            string[] streamids;

            var bytes = report.Render(
                renderFormat, deviceInfo,
                out mimeType, out encoding,
                out extension, out streamids, out warnings);

            return File(bytes, mimeType, $"{reportName}.{extension}");
        }

        private static string BuildDeviceInfo(string renderFormat)
        {
            if (string.Equals(renderFormat, "PDF", StringComparison.OrdinalIgnoreCase))
            {
                return "<DeviceInfo>" +
                       "<HumanReadablePDF>True</HumanReadablePDF>" +
                       "</DeviceInfo>";
            }
            if (string.Equals(renderFormat, "EXCELOPENXML", StringComparison.OrdinalIgnoreCase))
            {
                return "<DeviceInfo>" +
                       "<SimplePageHeaders>False</SimplePageHeaders>" +
                       "<SimplePageFooters>False</SimplePageFooters>" +
                       "<OmitDocumentMap>True</OmitDocumentMap>" +
                       "<RemoveSpaceBeforeContainer>False</RemoveSpaceBeforeContainer>" +
                       "<RemoveSpaceAfterContainer>False</RemoveSpaceAfterContainer>" +
                       "</DeviceInfo>";
            }
            if (string.Equals(renderFormat, "WORDOPENXML", StringComparison.OrdinalIgnoreCase))
            {
                return "<DeviceInfo>" +
                       "<ExpandToggles>False</ExpandToggles>" +
                       "</DeviceInfo>";
            }
            return null;
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
                    var cleanParams = parameters.ToDictionary(
                        p => p.Key,
                        p => string.IsNullOrEmpty(p.Value?.ToString()) ? DBNull.Value : (object)p.Value.ToString()
                    );

                    // Execute dataset query
                    DataTable dt = await _sqlDatasetExecutor.ExecuteQueryAsync(
                        connectionString,
                        dataset.CommandText,
                        dataset.CommandType,
                        cleanParams
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
