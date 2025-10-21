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
                // Log the exception instead of using ErrorHandler directly
                Console.WriteLine($"Error on Index page: {ex.Message}");
                // Add error message to TempData
                TempData["ErrorMessage"] = "An error occurred while loading the page.";
            }
        }
    }
}