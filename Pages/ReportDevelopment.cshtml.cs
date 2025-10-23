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
    public class ReportDevelopmentModel : PageModel
    {
        private readonly string _reportTemplatesPath = Path.Combine(Directory.GetCurrentDirectory(), "ReportTemplates", "RDLC");
        private readonly string _finalReportsPath = Path.Combine(Directory.GetCurrentDirectory(), "FinalReports");
        private readonly IConfiguration _configuration;

        public ReportDevelopmentModel(IConfiguration configuration)
        {
            _configuration = configuration;
            AvailableTemplates = new List<string>(); // Initialize to prevent null reference
        }

        [BindProperty]
        public string SelectedTemplate { get; set; }

        public List<string> AvailableTemplates { get; set; }

        public IActionResult OnGet()
        {
            AvailableTemplates = GetAvailableTemplates();
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


    public IActionResult OnPostLaunchReportBuilder(string templateName)
    {
        if (string.IsNullOrEmpty(templateName))
        {
            return BadRequest("Template name cannot be empty.");
        }

        string reportBuilderPath = _configuration["ReportBuilder:Path"];
        if (string.IsNullOrEmpty(reportBuilderPath))
        {
            return BadRequest("Report Builder path is not configured in appsettings.json.");
        }

        string rdlcFilePath = Path.Combine(_reportTemplatesPath, templateName + ".rdl");
        // Console.WriteLine($"rdlcFilePath: {rdlcFilePath}, templateName: {templateName}, reportBuilderPath: {reportBuilderPath}");
        if (!System.IO.File.Exists(rdlcFilePath))
        {
            return NotFound($"Report template '{templateName}.rdl' not found.");
        }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = reportBuilderPath,
                    Arguments = $"\"{rdlcFilePath}\"",
                    UseShellExecute = true,   // Needed for .exe apps
                    WorkingDirectory = Path.GetDirectoryName(reportBuilderPath)
                };

                Process.Start(psi);

                return new JsonResult(new { success = true, message = "Report Builder launched successfully." });
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Error launching Report Builder: {ex.Message}");
                // Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Error launching Report Builder: {ex.Message}";
                OnGet(); // Re-populate AvailableTemplates
                return Page();
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