using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ReportVisualizer.Security
{
    public class RequireScadaLoginAttribute : Attribute, IPageFilter
    {
        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            var svc = context.HttpContext.RequestServices.GetService(typeof(ScadaLoginService)) as ScadaLoginService;
            if (svc == null || !svc.Options.Enable) return;

            var pageName = context.ActionDescriptor.DisplayName ?? string.Empty;
            var path = context.HttpContext.Request.Path.Value ?? string.Empty;
            if (path.Contains("/Login", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("/DevMode", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("/Error", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!svc.IsLoggedIn(context.HttpContext))
            {
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToPageResult("/Login", new { returnUrl });
            }
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context) { }
        public void OnPageHandlerSelected(PageHandlerSelectedContext context) { }
    }
}
