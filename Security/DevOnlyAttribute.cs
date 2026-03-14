using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ReportVisualizer.Security
{
    public class DevOnlyAttribute : Attribute, IPageFilter
    {
        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            var mode = context.HttpContext.Session.GetString("AppMode");

            Console.WriteLine("DevOnly check. Session Mode = " + mode);

            if (mode != "DEV")
            {
                string script = "<script>alert('You do not have access. Redirecting to Report Viewer'); window.location.href = '/ReportViewer';</script>";
                context.Result = new ContentResult
                {
                    ContentType = "text/html",
                    Content = script
                };
            }
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context) { }
        public void OnPageHandlerSelected(PageHandlerSelectedContext context) { }
    }
}
