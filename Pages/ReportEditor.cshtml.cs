using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Reporting.NETCore;
using System.Text;

namespace ReportVisualizer.Pages
{
    public class ReportEditorModel : PageModel
    {
        private readonly string _reportTemplatesPath = Path.Combine(Directory.GetCurrentDirectory(), "ReportTemplates", "RDLC");

        [BindProperty]
        public string SelectedTemplate { get; set; }

        [BindProperty]
        public string ReportContent { get; set; }

        public IActionResult OnGet(string templateName, bool isNewTemplate = false)
        {
            if (isNewTemplate)
            {
                SelectedTemplate = templateName;
                ReportContent = "<Report xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition\" xmlns:rd=\"http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition\" Width=\"6in\" Name=\"Report1\"><Body><Height>2in</Height><ReportItems></ReportItems></Body><Width>6.5in</Width><Page><PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth><LeftMargin>1in</LeftMargin><RightMargin>1in</RightMargin><TopMargin>1in</TopMargin><BottomMargin>1in</BottomMargin><Style /></Page></Report>"; // Basic blank RDLC
            }
            else if (!string.IsNullOrEmpty(templateName))
            {
                SelectedTemplate = templateName;
                string rdlcFilePath = Path.Combine(_reportTemplatesPath, templateName + ".rdl");
                if (System.IO.File.Exists(rdlcFilePath))
                {
                    ReportContent = System.IO.File.ReadAllText(rdlcFilePath);
                }
                else
                {
                    ReportContent = $"Error: Report template '{templateName}.rdl' not found at '{rdlcFilePath}'.";
                }
            }
            else
            {
                // If no template is selected, start with a blank report
                ReportContent = "<Report xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition\" xmlns:rd=\"http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition\" Width=\"6in\" Name=\"Report1\"><Body><Height>2in</Height><ReportItems></ReportItems></Body><Width>6.5in</Width><Page><PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth><LeftMargin>1in</LeftMargin><RightMargin>1in</RightMargin><TopMargin>1in</TopMargin><BottomMargin>1in</BottomMargin><Style /></Page></Report>"; // Basic blank RDLC
            }
            return Page();
        }

        public IActionResult OnPostSaveReport(string reportContent, string reportName)
        {
            if (!string.IsNullOrEmpty(reportContent) && !string.IsNullOrEmpty(reportName))
            {
                if (!Directory.Exists(_reportTemplatesPath))
                {
                    Directory.CreateDirectory(_reportTemplatesPath);
                }
                System.IO.File.WriteAllText(Path.Combine(_reportTemplatesPath, reportName + ".rdl"), reportContent);
            }
            return RedirectToPage("/ReportDesigner"); // Redirect back to the designer page after saving
        }
    }
}