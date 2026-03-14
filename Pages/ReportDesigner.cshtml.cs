using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Data.Common;
using ReportVisualizer.Security;

namespace ReportVisualizer.Pages
{
    [DevOnly]
    public class ReportDesignerModel : PageModel
    {
        private readonly string _reportTemplatesPath = Path.Combine(Directory.GetCurrentDirectory(), "ReportTemplates", "RDLC");
        private readonly IConfiguration _configuration;

        public ReportDesignerModel(IConfiguration configuration)
        {
            _configuration = configuration;
            AvailableTemplates = GetAvailableTemplates(); // Initialize here
        }

        public List<string> AvailableTemplates { get; set; }
        public string SelectedTemplate { get; set; }

        public IActionResult OnGet(string templateName)
        {
            AvailableTemplates = GetAvailableTemplates();
            SelectedTemplate = templateName;
            return Page();
        }

        private string GetDatabaseName()
        {
            var connString = _configuration.GetConnectionString("DefaultConnection");

            var builder = new DbConnectionStringBuilder();
            builder.ConnectionString = connString;

            if (builder.TryGetValue("Database", out object db))
                return db.ToString();

            if (builder.TryGetValue("Initial Catalog", out object catalog))
                return catalog.ToString();

            return "UnknownDB";
        }


        public IActionResult OnPostLaunchReportBuilder(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                return new JsonResult(new { success = false, message = "Template name cannot be empty." });
            }

            string rdlcFilePath = Path.Combine(_reportTemplatesPath, templateName + ".rdl");
            Console.WriteLine($"rdlcFilePath: {rdlcFilePath}");
            if (!System.IO.File.Exists(rdlcFilePath))
            {
                return new JsonResult(new { success = false, message = $"Report template '{templateName}.rdl' not found." });
            }

            LaunchReportBuilderProcess(rdlcFilePath);
            return new JsonResult(new { success = true, message = $"Report Builder launched for {templateName}." });
        }

        public IActionResult OnPostCreateNewTemplateAndLaunchBuilder(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                return new JsonResult(new { success = false, message = "Template name cannot be empty." });
            }

            string newRdlcFilePath = Path.Combine(_reportTemplatesPath, templateName + ".rdl");

            if (System.IO.File.Exists(newRdlcFilePath))
            {
                return new JsonResult(new { success = false, message = $"Template '{templateName}' already exists. Please choose a different name or edit the existing one." });
            }

            try
            {
                // Get the database name from the connection string
                string databaseName = GetDatabaseName();
                

                string dataSourceXml = $@"
                                        <DataSources>
                                            <DataSource Name=""{databaseName}"">
                                            <rd:SecurityType>DataBase</rd:SecurityType>
                                            <ConnectionProperties>
                                                <DataProvider>SQL</DataProvider>
                                                <ConnectString>Data Source=.\SQLEXPRESS;Initial Catalog={databaseName}</ConnectString>
                                            </ConnectionProperties>
                                            <rd:DataSourceID>{Guid.NewGuid()}</rd:DataSourceID>
                                            </DataSource>
                                        </DataSources>";

                // Create an empty RDLC file
                // Create a minimal valid RDLC template
                string minimalRdlc = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report MustUnderstand=""df"" xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"" xmlns:df=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition/defaultfontfamily"" xmlns:am=""http://schemas.microsoft.com/sqlserver/reporting/authoringmetadata"">
  <rd:ReportUnitType>Inch</rd:ReportUnitType>
  <rd:ReportID>{Guid.NewGuid()}</rd:ReportID>
  <am:AuthoringMetadata>
    <am:CreatedBy>
      <am:Name>MSRB</am:Name>
      <am:Version>15.1.30001.02</am:Version>
    </am:CreatedBy>
    <am:UpdatedBy>
      <am:Name>MSRB</am:Name>
      <am:Version>15.1.30001.02</am:Version>
    </am:UpdatedBy>
    <am:LastModifiedTimestamp>2026-02-15T03:20:47.0271057Z</am:LastModifiedTimestamp>
  </am:AuthoringMetadata>
  <df:DefaultFontFamily>Segoe UI</df:DefaultFontFamily>
  <AutoRefresh>0</AutoRefresh>
  {dataSourceXml}
  <ReportSections>
    <ReportSection>
      <Body>
        <Height>2.25in</Height>
        <Style>
          <Border>
            <Style>None</Style>
          </Border>
        </Style>
      </Body>
      <Width>6in</Width>
      <Page>
        <PageFooter>
          <Height>0.45in</Height>
          <PrintOnFirstPage>true</PrintOnFirstPage>
          <PrintOnLastPage>true</PrintOnLastPage>
          <Style>
            <Border>
              <Style>None</Style>
            </Border>
          </Style>
        </PageFooter>
        <LeftMargin>1in</LeftMargin>
        <RightMargin>1in</RightMargin>
        <TopMargin>1in</TopMargin>
        <BottomMargin>1in</BottomMargin>
        <Style />
      </Page>
    </ReportSection>
  </ReportSections>
  <ReportParametersLayout>
    <GridLayoutDefinition>
      <NumberOfColumns>4</NumberOfColumns>
      <NumberOfRows>2</NumberOfRows>
    </GridLayoutDefinition>
  </ReportParametersLayout>
</Report>";
                System.IO.File.WriteAllText(newRdlcFilePath, minimalRdlc);
                LaunchReportBuilderProcess(newRdlcFilePath);
                return new JsonResult(new { success = true, message = $"New template '{templateName}' created and Report Builder launched." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error creating new template: {ex.Message}" });
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

        private List<string> GetAvailableTemplates()
        {
            if (!Directory.Exists(_reportTemplatesPath))
            {
                Directory.CreateDirectory(_reportTemplatesPath);
            }
            return Directory.GetFiles(_reportTemplatesPath, "*.rdl")
                            .Select(Path.GetFileNameWithoutExtension)
                            .ToList();
        }
    }
}