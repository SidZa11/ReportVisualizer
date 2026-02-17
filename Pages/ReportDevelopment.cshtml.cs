using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Reporting.NETCore;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace ReportVisualizer.Pages
{
[ValidateAntiForgeryToken]
    public class ReportDevelopmentModel : PageModel
    {
        private readonly string _reportTemplatesPath = Path.Combine(Directory.GetCurrentDirectory(), "ReportTemplates", "RDLC");
        private readonly string _finalReportsPath = Path.Combine(Directory.GetCurrentDirectory(), "ReportViewer", "Reports");
        private readonly IConfiguration _configuration;

        public ReportDevelopmentModel(IConfiguration configuration)
        {
            _configuration = configuration;
            AvailableTemplates = new List<string>(); // Initialize to prevent null reference
        }

        [BindProperty]
        public string SelectedTemplate { get; set; }

        public List<string> AvailableTemplates { get; set; }

        public IActionResult OnGet(string reportName)
        {
            // load templates for dropdown
            AvailableTemplates = GetAvailableTemplates();

            // if redirected from edit flow → open report builder
            if (TempData["LaunchReportPath"] != null)
            {
                string path = TempData["LaunchReportPath"].ToString();
                LaunchReportBuilderProcess(path);
            }

            return Page();
        }


        public JsonResult LaunchReportBuilder(string templateName)
        {
            try
            {
                Console.WriteLine(templateName);
                if (string.IsNullOrEmpty(templateName))
                {
                    return new JsonResult(new { success = false, message = "Template name cannot be empty." });
                }
                // Path to Report Builder executable
                string reportBuilderPath = @"C:\Program Files (x86)\Microsoft SQL Server Report Builder 15\ReportBuilder.exe";

                // Optional: Template path or parameters
                string templateFile = Path.Combine(AppContext.BaseDirectory, "ReportTemplates", "RDLC", $"{templateName}.rdl");
                if (!System.IO.File.Exists(templateFile))
                {
                    return new JsonResult(new { success = false, message = $"Report template '{templateName}.rdl' not found." });
                }
                
                // Prepare process start info
                var psi = new ProcessStartInfo
                {
                    FileName = reportBuilderPath,
                    Arguments = $"\"{templateFile}\"", // Launch with template file
                    UseShellExecute = true // Required to open desktop app
                };

                Process.Start(psi);

                return new JsonResult(new { success = true, message = $"Report Builder launched for {templateName}." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }


        public IActionResult OnPostLaunchReportBuilder(string reportName, string templateName)
        {
            if (string.IsNullOrEmpty(reportName) || string.IsNullOrEmpty(templateName))
                return BadRequest();

            string reportFilePath = Path.Combine(_finalReportsPath, reportName + ".rdl");
            string templateFilePath = Path.Combine(_reportTemplatesPath, templateName + ".rdl");
            
            if (!System.IO.File.Exists(reportFilePath))
            {
                if (!System.IO.File.Exists(templateFilePath))
                    return BadRequest("Template not found");

                System.IO.File.Copy(templateFilePath, reportFilePath, true);
            }

            // store path for next request
            // TempData["LaunchReportPath"] = reportFilePath;

            LaunchReportBuilderProcess(reportFilePath);

            return RedirectToPage("/ReportViewer", new { reportName = reportName });
        }



    private void LaunchReportBuilderProcess(string rdlPath)
    {
        if (!System.IO.File.Exists(rdlPath))
            return;

        var psi = new ProcessStartInfo
        {
            FileName = rdlPath,
            UseShellExecute = true
        };

        Process.Start(psi);
    }


    public IActionResult OnPostCreateReport(string templateName, string reportName)
    {
        Console.WriteLine("OnPostCreateReport method invoked.");
        Console.WriteLine($"Received - Template Name: {templateName}, Report Name: {reportName}");

        if (string.IsNullOrEmpty(templateName) || string.IsNullOrEmpty(reportName))
        {
            Console.WriteLine("Validation Error: Template name or report name is empty.");
            return new JsonResult(new { success = false, message = "Template name and report name cannot be empty." });
        }

        string sourceFilePath = Path.Combine(_reportTemplatesPath, templateName + ".rdl");
        string destinationDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ReportViewer", "Reports");
        string destinationFilePath = Path.Combine(destinationDirectory, reportName + ".rdl");

        Console.WriteLine($"Source File Path: {sourceFilePath}");
        Console.WriteLine($"Destination Directory: {destinationDirectory}");
        Console.WriteLine($"Destination File Path: {destinationFilePath}");

        try
        {
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            System.IO.File.Copy(sourceFilePath, destinationFilePath, true); // Overwrite if exists
            Console.WriteLine($"Report '{reportName}' created successfully at {destinationFilePath}");

            // Launch Report Builder with the newly created report
            LaunchReportBuilderProcess(destinationFilePath);

            return new JsonResult(new { success = true, redirectUrl = $"/ReportViewer?reportName={reportName}" });
        }
        catch (Exception ex)
        {
            // Log the exception details on the server-side for debugging
            Console.WriteLine($"Error saving report: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            return new JsonResult(new { success = false, message = $"Error saving report: {ex.Message}" });
        }
    }

        public JsonResult OnGetCheckReportExists(string reportName)
        {
            if (string.IsNullOrEmpty(reportName))
            {
                return new JsonResult(new { exists = false, message = "Report name cannot be empty." });
            }

            string reportFilePath = Path.Combine(_finalReportsPath, reportName + ".rdl");
            bool exists = System.IO.File.Exists(reportFilePath);
            return new JsonResult(new { exists = exists });
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