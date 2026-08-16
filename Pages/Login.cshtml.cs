using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportVisualizer.Security;
using ReportVisualizer.Infrastructure;

namespace ReportVisualizer.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ScadaLoginService _scadaLogin;
        private readonly LogoService _logoService;

        public LoginModel(ScadaLoginService scadaLogin, LogoService logoService)
        {
            _scadaLogin = scadaLogin;
            _logoService = logoService;
        }

        public bool LoginEnabled => _scadaLogin.Options.Enable;

        public LogoResolutionResult ClientLogo { get; set; }

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public IActionResult OnGet(string returnUrl = null)
        {
            ClientLogo = _logoService.ResolveClientLogo();

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
            ClientLogo = _logoService.ResolveClientLogo();

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
