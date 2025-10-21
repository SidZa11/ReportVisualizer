using System;
using System.Collections.Generic;
using System.IO;
using ReportVisualizer.ReportTemplates.TemplateEditor;
using ReportVisualizer.Utilities;

namespace ReportVisualizer.ReportDevelopment
{
    /// <summary>
    /// UI component for template selection
    /// </summary>
    public class TemplateSelector
    {
        private readonly TemplateBuilder _templateBuilder;
        
        public TemplateSelector()
        {
            _templateBuilder = new TemplateBuilder();
        }
        
        /// <summary>
        /// Gets a list of available templates
        /// </summary>
        /// <returns>List of template names</returns>
        public List<string> GetAvailableTemplates()
        {
            try
            {
                string[] templates = _templateBuilder.GetAvailableTemplates();
                return new List<string>(templates);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error retrieving templates: {ex.Message}");
                return new List<string>();
            }
        }
        
        /// <summary>
        /// Gets the content of a template
        /// </summary>
        /// <param name="templateName">Name of the template</param>
        /// <returns>Template content</returns>
        public string GetTemplateContent(string templateName)
        {
            try
            {
                // Ensure template name has .rdlc extension
                if (!templateName.EndsWith(".rdlc", StringComparison.OrdinalIgnoreCase))
                {
                    templateName += ".rdlc";
                }
                
                string templatesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReportTemplates", "PredefinedTemplates");
                string templatePath = Path.Combine(templatesDirectory, templateName);
                
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Template '{templateName}' not found");
                }
                
                string content = File.ReadAllText(templatePath);
                Logger.Log($"Retrieved content for template '{templateName}'");
                
                return content;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error retrieving template content: {ex.Message}");
                throw;
            }
        }
    }
}