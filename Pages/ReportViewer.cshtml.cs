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
        public bool ShowExcelExport { get; set; } = true;
        public bool ShowPdfExport { get; set; } = true;
        public bool ShowPrint { get; set; } = true;
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
            ShowExcelExport = string.Equals(_configuration["ReportDownload:excel:enable"], "true", StringComparison.OrdinalIgnoreCase);
            ShowPdfExport = string.Equals(_configuration["ReportDownload:pdf:enable"], "true", StringComparison.OrdinalIgnoreCase);
            ShowPrint = string.Equals(_configuration["ReportDownload:print:enable"], "true", StringComparison.OrdinalIgnoreCase);

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
            ShowExcelExport = string.Equals(_configuration["ReportDownload:excel:enable"], "true", StringComparison.OrdinalIgnoreCase);
            ShowPdfExport = string.Equals(_configuration["ReportDownload:pdf:enable"], "true", StringComparison.OrdinalIgnoreCase);
            ShowPrint = string.Equals(_configuration["ReportDownload:print:enable"], "true", StringComparison.OrdinalIgnoreCase);

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

            bool allowExcel = string.Equals(_configuration["ReportDownload:excel:enable"], "true", StringComparison.OrdinalIgnoreCase);
            bool allowPdf = string.Equals(_configuration["ReportDownload:pdf:enable"], "true", StringComparison.OrdinalIgnoreCase);
            if ((string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase) && !allowExcel) ||
                (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase) && !allowPdf))
            {
                return Forbid();
            }

            var reportPath = Path.Combine(_reportsPath, reportName + ".rdl");

            if (!System.IO.File.Exists(reportPath))
                return BadRequest("Report not found");

            LocalReport report = new();

            string renderFormat = format.ToLower() switch
            {
                "pdf" => "PDF",
                "excel" => "EXCELOPENXML",
                "word" => "WORDOPENXML",
                _ => "PDF"
            };

            bool isExcel = string.Equals(renderFormat, "EXCELOPENXML", StringComparison.OrdinalIgnoreCase);

            if (isExcel)
            {
                // In Excel, PageFooter is not rendered as worksheet rows (only in print layout/setup).
                // To guarantee the footer is visible as rows in the sheet, we move PageFooter ReportItems
                // to the bottom of Body and drop the PageFooter element for this render pass.
                try
                {
                    using var fs = new FileStream(reportPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs);
                    var rdl = reader.ReadToEnd();
                    var transformed = TransformRdlMovePageFooterToBodyForExcel(rdl);
                    using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(transformed));
                    report.LoadReportDefinition(ms);
                }
                catch
                {
                    // Fall back to the raw RDL if transform fails.
                    report.ReportPath = reportPath;
                }
            }
            else
            {
                report.ReportPath = reportPath;
            }

            Console.WriteLine($"OnGetExport called for report: {reportName}, format: {format}");

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

        public async Task<IActionResult> OnGetPrintPdf(string reportName)
        {
            bool allowPrint = string.Equals(_configuration["ReportDownload:print:enable"], "true", StringComparison.OrdinalIgnoreCase);
            if (!allowPrint)
            {
                return Forbid();
            }

            if (string.IsNullOrEmpty(reportName))
                return BadRequest("Report not selected");

            var reportPath = Path.Combine(_reportsPath, reportName + ".rdl");

            if (!System.IO.File.Exists(reportPath))
                return BadRequest("Report not found");

            LocalReport report = new();
            report.ReportPath = reportPath;

            Console.WriteLine($"OnGetPrintPdf called for report: {reportName}");

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

                Console.WriteLine("Print - Report Parameters being passed to LoadReportDataAsync:");
                foreach (var param in reportParameters)
                {
                    Console.WriteLine($"- {param.Key}: {param.Value}");
                }

                await LoadReportDataAsync(report, reportPath, reportParameters);
            }

            string deviceInfo = BuildDeviceInfo("PDF");

            string mimeType, encoding, extension;
            Warning[] warnings;
            string[] streamids;

            var bytes = report.Render(
                "PDF", deviceInfo,
                out mimeType, out encoding,
                out extension, out streamids, out warnings);

            Response.Headers["Content-Disposition"] = "inline; filename=\"" + reportName + ".pdf\"";
            return File(bytes, mimeType);
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

        private static string BuildDeviceInfo(string renderFormat)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<DeviceInfo>");
            if (string.Equals(renderFormat, "PDF", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("<HumanReadablePDF>True</HumanReadablePDF>");
                sb.Append("<EmbedFonts>None</EmbedFonts>");
            }
            else if (string.Equals(renderFormat, "EXCELOPENXML", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(renderFormat, "EXCEL", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("<OmitDocumentMap>True</OmitDocumentMap>");
                sb.Append("<RemoveSpaceBeforeFootnote>True</RemoveSpaceBeforeFootnote>");
                sb.Append("<SimplePageHeaders>False</SimplePageHeaders>");
                sb.Append("<PrintOnFirstPage>True</PrintOnFirstPage>");
                sb.Append("<PrintOnLastPage>True</PrintOnLastPage>");
                sb.Append("<InsertPageBreaks>True</InsertPageBreaks>");
            }
            else if (string.Equals(renderFormat, "WORDOPENXML", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(renderFormat, "WORD", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("<OmitDocumentMap>True</OmitDocumentMap>");
                sb.Append("<ExpandToggles>True</ExpandToggles>");
            }
            sb.Append("</DeviceInfo>");
            return sb.ToString();
        }

        private static string TransformRdlMovePageFooterToBodyForExcel(string rdl)
        {
            if (string.IsNullOrWhiteSpace(rdl)) return rdl;
            var xdoc = System.Xml.Linq.XDocument.Parse(rdl);
            var ns = xdoc.Root?.Name.Namespace ?? System.Xml.Linq.XNamespace.None;
            var page = xdoc.Root?.Element(ns + "Page");
            if (page == null) return rdl;
            var pageFooter = page.Element(ns + "PageFooter");
            if (pageFooter == null) return rdl;
            var footerItems = pageFooter.Element(ns + "ReportItems");
            if (footerItems == null || !footerItems.HasElements)
            {
                pageFooter.Remove();
                return xdoc.ToString();
            }
            var body = xdoc.Root?.Element(ns + "Body");
            if (body == null) return rdl;

            var bodyItems = body.Element(ns + "ReportItems");
            if (bodyItems == null)
            {
                bodyItems = new System.Xml.Linq.XElement(ns + "ReportItems");
                body.AddFirst(bodyItems);
            }

            static double InchesStringToDouble(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return 0.0;
                s = s.Trim();
                if (s.EndsWith("in", StringComparison.OrdinalIgnoreCase)) s = s.Substring(0, s.Length - 2);
                else if (s.EndsWith("cm", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(s.Substring(0, s.Length - 2), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var cm))
                        return cm / 2.54;
                    return 0;
                }
                else if (s.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(s.Substring(0, s.Length - 2), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var mm))
                        return mm / 25.4;
                    return 0;
                }
                else if (s.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(s.Substring(0, s.Length - 2), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var pt))
                        return pt / 72.0;
                    return 0;
                }
                if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
                return 0;
            }
            static string DoubleToInchesString(double v) => v.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + "in";

            double maxBodyBottom = 0;
            foreach (var el in bodyItems.Elements())
            {
                double top = InchesStringToDouble((string)el.Element(ns + "Top"));
                double height = InchesStringToDouble((string)el.Element(ns + "Height"));
                double bottom = top + height;
                if (bottom > maxBodyBottom) maxBodyBottom = bottom;
            }

            double topOffset = maxBodyBottom + 0.1;
            double footerBottom = topOffset;
            double maxFooterWidth = 0;

            // Wrap the moved footer content in a separator rectangle (subtle dashed line) so it reads as a footer in Excel
            string footerWidthStr = (string)pageFooter.Element(ns + "Height");
            double footerHeight = InchesStringToDouble(footerWidthStr);

            var separatorTop = topOffset;
            var separatorHeight = 0.03;
            var separator = new System.Xml.Linq.XElement(ns + "Rectangle",
                new System.Xml.Linq.XAttribute("Name", "__ExcelFooterSeparator_" + Guid.NewGuid().ToString("N").Substring(0, 8)),
                new System.Xml.Linq.XElement(ns + "Top", DoubleToInchesString(separatorTop)),
                new System.Xml.Linq.XElement(ns + "Left", "0in"),
                new System.Xml.Linq.XElement(ns + "Height", DoubleToInchesString(separatorHeight)),
                new System.Xml.Linq.XElement(ns + "Width", DoubleToInchesString(7.0)),
                new System.Xml.Linq.XElement(ns + "Style",
                    new System.Xml.Linq.XElement(ns + "Border",
                        new System.Xml.Linq.XElement(ns + "Style", "Solid"),
                        new System.Xml.Linq.XElement(ns + "Color", "SlateGray")
                    ),
                    new System.Xml.Linq.XElement(ns + "TopBorder",
                        new System.Xml.Linq.XElement(ns + "Style", "Dashed"),
                        new System.Xml.Linq.XElement(ns + "Color", "SlateGray"),
                        new System.Xml.Linq.XElement(ns + "Width", "0.5pt")
                    )
                )
            );
            bodyItems.Add(separator);
            topOffset += separatorHeight + 0.05;

            foreach (var item in footerItems.Elements().ToList())
            {
                double oldTop = InchesStringToDouble((string)item.Element(ns + "Top"));
                double oldHeight = InchesStringToDouble((string)item.Element(ns + "Height"));
                double oldLeft = InchesStringToDouble((string)item.Element(ns + "Left"));
                double oldWidth = InchesStringToDouble((string)item.Element(ns + "Width"));

                double newTop = topOffset + oldTop;
                var topEl = item.Element(ns + "Top");
                if (topEl == null)
                {
                    topEl = new System.Xml.Linq.XElement(ns + "Top");
                    item.AddFirst(topEl);
                }
                topEl.Value = DoubleToInchesString(newTop);

                // Also strip page-number aggregate expressions that only work in page sections,
                // by replacing them with a human-readable placeholder (Excel has no "pages").
                foreach (var valEl in item.Descendants(ns + "Value"))
                {
                    string v = (string)valEl;
                    if (!string.IsNullOrWhiteSpace(v))
                    {
                        string nv = v
                            .Replace("Globals!PageNumber", "\"\"")
                            .Replace("Globals!TotalPages", "\"\"")
                            .Replace("Globals!PageName", "\"Report Footer\"");
                        if (nv != v) valEl.Value = nv;
                    }
                }

                item.Remove();
                bodyItems.Add(item);

                double itemBottom = newTop + oldHeight;
                if (itemBottom > footerBottom) footerBottom = itemBottom;
                double itemRight = oldLeft + oldWidth;
                if (itemRight > maxFooterWidth) maxFooterWidth = itemRight;
            }
            topOffset = footerBottom;

            // Grow body Height to include the added footer rows
            var bodyHeightEl = body.Element(ns + "Height");
            double bodyHeight = InchesStringToDouble((string)bodyHeightEl);
            if (topOffset > bodyHeight)
            {
                if (bodyHeightEl == null)
                {
                    bodyHeightEl = new System.Xml.Linq.XElement(ns + "Height");
                    body.Add(bodyHeightEl);
                }
                bodyHeightEl.Value = DoubleToInchesString(topOffset + 0.1);
            }

            pageFooter.Remove();
            return xdoc.ToString();
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
