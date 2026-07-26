using Microsoft.Reporting.NETCore;
using System;
using System.IO;
using System.Text;
using System.Data;
using ReportVisualizer.ReportViewer.ReportDataExtraction;
using ReportVisualizer.ReportViewer.ReportDataExecution;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace ReportVisualizer.ReportViewer
{
    public class RenderedReportResult
    {
        public string Html { get; set; }
        public RenderedReportSnapshot Snapshot { get; set; }
    }

    public class ReportRenderer
    {
        private readonly RdlDataExtractor _rdlDataExtractor;
        private readonly SqlDatasetExecutor _sqlDatasetExecutor;
        private readonly IConfiguration _configuration;

        public ReportRenderer(RdlDataExtractor rdlDataExtractor, SqlDatasetExecutor sqlDatasetExecutor, IConfiguration configuration)
        {
            _rdlDataExtractor = rdlDataExtractor;
            _sqlDatasetExecutor = sqlDatasetExecutor;
            _configuration = configuration;
        }

        public async Task<string> RenderReport(string reportPath, Dictionary<string, object> parameters = null)
        {
            var result = await RenderReportWithSnapshot(reportPath, parameters);
            return result.Html;
        }

        public async Task<RenderedReportResult> RenderReportWithSnapshot(string reportPath, Dictionary<string, object> parameters = null)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var snapshot = new RenderedReportSnapshot
            {
                ReportName = Path.GetFileNameWithoutExtension(reportPath),
                Parameters = parameters == null
                    ? new Dictionary<string, object>()
                    : new Dictionary<string, object>(parameters)
            };

            using (var preprocessedStream = RdlPreprocessor.PreprocessFile(reportPath, new RdlPreprocessOptions { ForceRepeatHeaderRowsOnEveryPage = true }))
            using (var localReport = new LocalReport())
            {
                localReport.LoadReportDefinition(preprocessedStream);
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
                    DataTable dataTable = new DataTable();
                    if (dataSource != null)
                    {
                        Console.WriteLine($"DataSource '{dataSource.Name}' found for DataSet '{dataSet.Name}'.");
                        try
                        {
                            dataTable = await _sqlDatasetExecutor.ExecuteQueryAsync(
                                _configuration.GetConnectionString("DefaultConnection"),
                                dataSet.CommandText,
                                dataSet.CommandType,
                                parameters
                            );
                            dataTable.TableName = dataSet.Name;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error executing dataset '{dataSet.Name}': {ex.Message}");
                            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                            dataTable = new DataTable(dataSet.Name);
                            try { localReport.SetParameters(new ReportParameter("DataSetError_" + dataSet.Name, ex.Message)); } catch { }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"DataSource '{dataSet.DataSourceName}' not found for DataSet '{dataSet.Name}'.");
                        dataTable = new DataTable(dataSet.Name);
                    }

                    localReport.DataSources.Add(new ReportDataSource(dataSet.Name, dataTable));
                    snapshot.DataSources.Add(new ReportDataSourceSnapshot
                    {
                        Name = dataSet.Name,
                        Table = DataTableDtoMapper.ToDto(dataTable)
                    });
                }

                byte[] html = localReport.Render("HTML5");
                return new RenderedReportResult
                {
                    Html = Encoding.UTF8.GetString(html),
                    Snapshot = snapshot
                };
            }
        }

        public static void ExportReport(string reportPath, string exportFormat)
        {
            Console.WriteLine($"Exporting {reportPath} to {exportFormat}");
        }
    }
}