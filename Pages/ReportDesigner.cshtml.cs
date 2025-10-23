using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Reporting.NETCore;
using System.Text;

namespace ReportVisualizer.Pages
{
    public class ReportDesignerModel : PageModel
    {
        private readonly string _reportTemplatesPath = Path.Combine(Directory.GetCurrentDirectory(), "ReportTemplates", "RDLC");
        private readonly string _finalReportsPath = Path.Combine(Directory.GetCurrentDirectory(), "FinalReports");

        [BindProperty]
        public string SelectedTemplate { get; set; }

        [BindProperty]
        public string ReportContent { get; set; }

        public List<string> AvailableTemplates { get; set; }

        public IActionResult OnGet(string templateName)
        {
            AvailableTemplates = GetAvailableTemplates();
            if (!string.IsNullOrEmpty(templateName))
            {
                SelectedTemplate = templateName;
            }
            return Page();
        }

        public ContentResult OnGetLoadReportContent(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                return Content("Template name cannot be empty.");
            }

            string rdlcFilePath = Path.Combine(_reportTemplatesPath, templateName + ".rdl");

            if (!System.IO.File.Exists(rdlcFilePath))
            {
                return Content($"Report template '{templateName}.rdl' not found.");
            }

            return Content(System.IO.File.ReadAllText(rdlcFilePath));
        }

        public IActionResult OnPostSaveReport(string reportContent, string reportName)
        {
            if (!string.IsNullOrEmpty(reportContent) && !string.IsNullOrEmpty(reportName))
            {
                if (!Directory.Exists(_finalReportsPath))
                {
                    Directory.CreateDirectory(_finalReportsPath);
                }
                System.IO.File.WriteAllText(Path.Combine(_finalReportsPath, reportName + ".rdl"), reportContent);
            }
            return Page();
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