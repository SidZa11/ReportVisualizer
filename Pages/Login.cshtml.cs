using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportVisualizer.Security;

namespace ReportVisualizer.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ScadaLoginService _scadaLogin;

        public LoginModel(ScadaLoginService scadaLogin)
        {
            _scadaLogin = scadaLogin;
        }

        public bool LoginEnabled => _scadaLogin.Options.Enable;

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public IActionResult OnGet(string returnUrl = null)
        {
            if (!_scadaLogin.Options.Enable)
            {
                return Redirect(returnUrl ?? "/ReportViewer");
            }
            if (_scadaLogin.IsLoggedIn(HttpContext))
            {
                return Redirect(returnUrl ?? "/ReportViewer");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return Page();
        }

        public IActionResult OnPost(string returnUrl = null)
        {
            if (!_scadaLogin.Options.Enable)
            {
                return Redirect(returnUrl ?? "/ReportViewer");
            }

            var result = _scadaLogin.ValidateCredentials(Username, Password);
            if (!result.Success)
            {
                ErrorMessage = result.Message;
                ViewData["ReturnUrl"] = returnUrl;
                return Page();
            }

            _scadaLogin.CreateLoginSession(HttpContext, result.Username, result.FullName);
            return Redirect(returnUrl ?? "/ReportViewer");
        }

        public IActionResult OnPostLogout(string returnUrl = null)
        {
            _scadaLogin.ClearLoginSession(HttpContext);
            if (_scadaLogin.Options.Enable)
            {
                return RedirectToPage("/Login", new { returnUrl });
            }
            return Redirect(returnUrl ?? "/ReportViewer");
        }
    }
}
