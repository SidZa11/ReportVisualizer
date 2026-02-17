using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace ReportVisualizer.ReportViewer.ReportDataExtraction
{
    public class RdlDataExtractor
    {
        public List<DataSourceInfo> ExtractDataSources(string rdlFilePath)
        {
            var rdlDocument = XDocument.Load(rdlFilePath);
            Console.WriteLine("Extracting DataSources...");
            var dataSources = new List<DataSourceInfo>();
            var dataSourceElements = rdlDocument.Descendants(rdlDocument.Root.Name.Namespace + "DataSources").Elements(rdlDocument.Root.Name.Namespace + "DataSource");

            foreach (var dsElement in dataSourceElements)
            {
                var name = dsElement.Attribute("Name")?.Value;
                var connectionProperties = dsElement.Element(rdlDocument.Root.Name.Namespace + "ConnectionProperties");
                var dataProvider = connectionProperties?.Element(rdlDocument.Root.Name.Namespace + "DataProvider")?.Value;
                var connectString = connectionProperties?.Element(rdlDocument.Root.Name.Namespace + "ConnectString")?.Value;

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(dataProvider) && !string.IsNullOrEmpty(connectString))
                {
                    dataSources.Add(new DataSourceInfo
                    {
                        Name = name,
                        ConnectionString = connectString,
                        Provider = dataProvider
                    });
                }
            }
            Console.WriteLine($"Found {dataSources.Count} DataSources.");
            return dataSources;
        }

        public List<DataSetInfo> ExtractDataSets(string rdlFilePath)
        {
            var rdlDocument = XDocument.Load(rdlFilePath);
            Console.WriteLine("Extracting DataSets...");
            var dataSets = new List<DataSetInfo>();
            var dataSetElements = rdlDocument.Descendants(rdlDocument.Root.Name.Namespace + "DataSets").Elements(rdlDocument.Root.Name.Namespace + "DataSet");

            foreach (var dsElement in dataSetElements)
            {
                var name = dsElement.Attribute("Name")?.Value;
                var queryElement = dsElement.Element(rdlDocument.Root.Name.Namespace + "Query");
                var dataSourceName = queryElement?.Element(rdlDocument.Root.Name.Namespace + "DataSourceName")?.Value;
                var commandType = queryElement?.Element(rdlDocument.Root.Name.Namespace + "CommandType")?.Value;
                var commandText = queryElement?.Element(rdlDocument.Root.Name.Namespace + "CommandText")?.Value;

                Console.WriteLine($"  DataSet Name: {name}");
                Console.WriteLine($"  DataSource Name: {dataSourceName}");
                Console.WriteLine($"  Command Type: {commandType ?? "null"}");
                Console.WriteLine($"  Command Text: {commandText ?? "null"}");

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(dataSourceName) && !string.IsNullOrEmpty(commandText))
                {
                    var fields = new List<FieldInfo>();
                    var fieldsElement = dsElement.Element(rdlDocument.Root.Name.Namespace + "Fields");
                    if (fieldsElement != null)
                    {
                        foreach (var fieldElement in fieldsElement.Elements(rdlDocument.Root.Name.Namespace + "Field"))
                        {
                            var fieldName = fieldElement.Attribute("Name")?.Value;
                            if (!string.IsNullOrEmpty(fieldName))
                            {
                                fields.Add(new FieldInfo { Name = fieldName });
                            }
                        }
                    }

                    dataSets.Add(new DataSetInfo
                    {
                        Name = name,
                        DataSourceName = dataSourceName,
                        CommandType = commandType,
                        CommandText = commandText,
                        Fields = fields
                    });
                }
            }
            Console.WriteLine($"Found {dataSets.Count} DataSets.");
            return dataSets;
        }

        public List<QueryParameterInfo> ExtractQueryParameters(string rdlFilePath)
        {
            var rdlDocument = XDocument.Load(rdlFilePath);
            Console.WriteLine("Extracting QueryParameters...");
            var queryParameters = new List<QueryParameterInfo>();
            var reportParametersElements = rdlDocument.Descendants(rdlDocument.Root.Name.Namespace + "ReportParameters").Elements(rdlDocument.Root.Name.Namespace + "ReportParameter");

            foreach (var rpElement in reportParametersElements)
            {
                var name = rpElement.Attribute("Name")?.Value;
                var dataType = rpElement.Element(rdlDocument.Root.Name.Namespace + "DataType")?.Value;
                var prompt = rpElement.Element(rdlDocument.Root.Name.Namespace + "Prompt")?.Value;

                if (!string.IsNullOrEmpty(name))
                {
                    queryParameters.Add(new QueryParameterInfo
                    {
                        Name = name,
                        DataType = dataType,
                        Prompt = prompt
                    });
                }
            }
            Console.WriteLine($"Found {queryParameters.Count} QueryParameters.");
            return queryParameters;
        }
    }

    public class DataSourceInfo
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string Provider { get; set; }
    }

    public class DataSetInfo
    {
        public string Name { get; set; }
        public string DataSourceName { get; set; }
        public string CommandType { get; set; }
        public string CommandText { get; set; }
        public List<FieldInfo> Fields { get; set; }
    }

    public class FieldInfo
    {
        public string Name { get; set; }
    }

    public class QueryParameterInfo
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string Prompt { get; set; }
    }
}