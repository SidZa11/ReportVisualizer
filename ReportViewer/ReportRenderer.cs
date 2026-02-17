using Microsoft.Reporting.NETCore;
using System;
using System.IO;
using System.Text;
using System.Data;
using ReportVisualizer.ReportViewer.ReportDataExtraction;
using ReportVisualizer.ReportViewer.ReportDataExecution;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration; // Add this using statement

namespace ReportVisualizer.ReportViewer
{
    public class ReportRenderer
    {
        private readonly RdlDataExtractor _rdlDataExtractor;
        private readonly SqlDatasetExecutor _sqlDatasetExecutor;
        private readonly IConfiguration _configuration; // Declare IConfiguration field

        public ReportRenderer(RdlDataExtractor rdlDataExtractor, SqlDatasetExecutor sqlDatasetExecutor, IConfiguration configuration)
        {
            _rdlDataExtractor = rdlDataExtractor;
            _sqlDatasetExecutor = sqlDatasetExecutor;
            _configuration = configuration; // Initialize IConfiguration field
        }

        public async Task<string> RenderReport(string reportPath, Dictionary<string, object> parameters = null)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var reportFileStream = new FileStream(reportPath, FileMode.Open, FileAccess.Read))
            {
                var localReport = new LocalReport();
                localReport.LoadReportDefinition(reportFileStream);
                if (parameters != null && parameters.Any())
                {
                    var reportParameters = parameters.Select(p =>
                    {
                        if (p.Value is Microsoft.Extensions.Primitives.StringValues sv)
                            return new ReportParameter(p.Key, sv.ToArray());

                        return new ReportParameter(p.Key, p.Value?.ToString());
                    }).ToList();

                    localReport.SetParameters(reportParameters);
                    Console.WriteLine($"Set parameters for report {reportPath}: {string.Join(", ", reportParameters.Select(p => $"{p.Name}={string.Join(",", p.Values)}"))}");
                }

                var dataSourcesInfo = _rdlDataExtractor.ExtractDataSources(reportPath);
                var dataSetsInfo = _rdlDataExtractor.ExtractDataSets(reportPath);
                
                Console.WriteLine($"Extracted {dataSourcesInfo.Count} DataSources and {dataSetsInfo.Count} DataSets.");

                foreach (var dataSet in dataSetsInfo)
                {
                    Console.WriteLine($"Processing DataSet: {dataSet.Name}");
                    var dataSource = dataSourcesInfo.FirstOrDefault(ds => ds.Name == dataSet.DataSourceName);
                    if (dataSource != null)
                    {
                        Console.WriteLine($"DataSource '{dataSource.Name}' found for DataSet '{dataSet.Name}'.");
                        try
                        {
                            var dataTable = await _sqlDatasetExecutor.ExecuteQueryAsync(
                                _configuration.GetConnectionString("DefaultConnection"),
                                dataSet.CommandText,
                                dataSet.CommandType,
                                parameters
                            );
                            localReport.DataSources.Add(new ReportDataSource(dataSet.Name, dataTable));
                        }
                        catch (Exception ex)
                        {
                            // Log the error and add an empty DataTable to prevent report crash
                            Console.WriteLine($"Error executing dataset '{dataSet.Name}': {ex.Message}");
                            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                            localReport.DataSources.Add(new ReportDataSource(dataSet.Name, new DataTable()));
                            // Optionally, you can add a parameter to the report to indicate an error
                            localReport.SetParameters(new ReportParameter("DataSetError_" + dataSet.Name, ex.Message));
                        }
                    }
                    else
                    {
                        Console.WriteLine($"DataSource '{dataSet.DataSourceName}' not found for DataSet '{dataSet.Name}'.");
                        localReport.DataSources.Add(new ReportDataSource(dataSet.Name, new DataTable()));
                    }
                }

                byte[] html = localReport.Render("HTML5");
                return Encoding.UTF8.GetString(html);
            }
        }

        public static void ExportReport(string reportPath, string exportFormat)
        {
            // Logic to export the report
            Console.WriteLine($"Exporting {reportPath} to {exportFormat}");
        }
    }
}