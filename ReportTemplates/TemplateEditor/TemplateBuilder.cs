using System;
using System.IO;
using System.Xml;
using ReportVisualizer.Utilities;

namespace ReportVisualizer.ReportTemplates.TemplateEditor
{
    /// <summary>
    /// Handles the creation and modification of report templates
    /// </summary>
    public class TemplateBuilder
    {
        private readonly string _templatesDirectory;
        
        public TemplateBuilder()
        {
            _templatesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReportTemplates", "PredefinedTemplates");
            
            // Ensure templates directory exists
            if (!Directory.Exists(_templatesDirectory))
            {
                Directory.CreateDirectory(_templatesDirectory);
                Logger.Log($"Created templates directory: {_templatesDirectory}");
            }
        }
        
        /// <summary>
        /// Creates a new report template
        /// </summary>
        /// <param name="templateName">Name of the template</param>
        /// <param name="templateContent">XML content of the template</param>
        /// <returns>Path to the created template file</returns>
        public string CreateTemplate(string templateName, string templateContent)
        {
            try
            {
                // Validate template name
                if (string.IsNullOrWhiteSpace(templateName))
                {
                    throw new ArgumentException("Template name cannot be empty");
                }
                
                // Ensure template name has .rdlc extension
                if (!templateName.EndsWith(".rdlc", StringComparison.OrdinalIgnoreCase))
                {
                    templateName += ".rdlc";
                }
                
                // Create template file path
                string templatePath = Path.Combine(_templatesDirectory, templateName);
                
                // Check if template already exists
                if (File.Exists(templatePath))
                {
                    throw new InvalidOperationException($"Template '{templateName}' already exists");
                }
                
                // Validate template content
                TemplateValidator validator = new TemplateValidator();
                if (!validator.ValidateTemplate(templateContent))
                {
                    throw new ArgumentException("Invalid template content");
                }
                
                // Write template to file
                File.WriteAllText(templatePath, templateContent);
                Logger.Log($"Created template: {templateName}");
                
                return templatePath;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error creating template '{templateName}': {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Updates an existing template
        /// </summary>
        /// <param name="templateName">Name of the template</param>
        /// <param name="templateContent">New XML content</param>
        /// <returns>True if successful</returns>
        public bool UpdateTemplate(string templateName, string templateContent)
        {
            try
            {
                // Ensure template name has .rdlc extension
                if (!templateName.EndsWith(".rdl", StringComparison.OrdinalIgnoreCase))
                {
                    templateName += ".rdl";
                }
                
                // Create template file path
                string templatePath = Path.Combine(_templatesDirectory, templateName);
                
                // Check if template exists
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Template '{templateName}' not found");
                }
                
                // Validate template content
                TemplateValidator validator = new TemplateValidator();
                if (!validator.ValidateTemplate(templateContent))
                {
                    throw new ArgumentException("Invalid template content");
                }
                
                // Write updated template to file
                File.WriteAllText(templatePath, templateContent);
                Logger.Log($"Updated template: {templateName}");
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error updating template '{templateName}': {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets a list of available templates
        /// </summary>
        /// <returns>Array of template names</returns>
        public string[] GetAvailableTemplates()
        {
            try
            {
                // Get all .rdlc files in the templates directory
                string[] templateFiles = Directory.GetFiles(_templatesDirectory, "*.rdl");
                
                // Extract file names
                for (int i = 0; i < templateFiles.Length; i++)
                {
                    templateFiles[i] = Path.GetFileName(templateFiles[i]);
                }
                
                Logger.Log($"Retrieved {templateFiles.Length} available templates");
                return templateFiles;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error retrieving available templates: {ex.Message}");
                throw;
            }
        }
    }
}