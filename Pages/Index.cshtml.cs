using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportVisualizer.Utilities;
using System;

namespace ReportVisualizer.Pages
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            try
            {
                // Initialize any required components for the home page
                // This could include checking database connection status, etc.
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, this);
            }
        }
    }
}