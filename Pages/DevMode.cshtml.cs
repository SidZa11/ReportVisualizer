using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using ReportVisualizer.Security;

namespace ReportVisualizer.Pages
{
    /// <summary>
    /// Handles development mode login and logout
    /// </summary>
    public class DevModeModel : PageModel
    {
        private readonly IConfiguration _config;

        public DevModeModel(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Handles POST request for login
        /// </summary>
        /// <param name="password">Password from form</param>
        /// <returns>Success or fail message</returns>
        public IActionResult OnPostLogin(string password)
        {
            var pwd = _config["DevMode:Password"];

            if (password == pwd)
            {
                ModeManager.SetDev(HttpContext);
                Console.WriteLine("DEV MODE ENABLED");
                return Content("OK");
            }

            Console.WriteLine("DEV LOGIN FAILED");
            return Content("FAIL");
        }


        public IActionResult OnPostLogout()
        {
            ModeManager.Logout(HttpContext);
            return Content("OK");
        }

        public class PasswordDto
        {
            public string Password { get; set; }
        }
    }
}
