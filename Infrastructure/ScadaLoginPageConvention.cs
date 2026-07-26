using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ReportVisualizer.Infrastructure
{
    public class ScadaLoginPageConvention : IPageApplicationModelConvention
    {
        public void Apply(PageApplicationModel model)
        {
            var anon = new[] { "/Login", "/DevMode", "/Error" };
            var pagePath = model.ViewEnginePath ?? string.Empty;
            if (anon.Any(p => pagePath.Equals(p, System.StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
            var filter = new Security.RequireScadaLoginAttribute();
            if (!model.Filters.Any(f => f is Security.RequireScadaLoginAttribute))
            {
                model.Filters.Add(filter);
            }
        }
    }
}
