using System;
using System.Xml;
using ReportVisualizer.Utilities;

namespace ReportVisualizer.ReportTemplates.TemplateEditor
{
    /// <summary>
    /// Validates report templates
    /// </summary>
    public class TemplateValidator
    {
        /// <summary>
        /// Validates a template's XML content
        /// </summary>
        /// <param name="templateContent">XML content to validate</param>
        /// <returns>True if valid</returns>
        public bool ValidateTemplate(string templateContent)
        {
            try
            {
                // Check if content is empty
                if (string.IsNullOrWhiteSpace(templateContent))
                {
                    Logger.LogWarning("Template validation failed: Empty content");
                    return false;
                }

                // Try to parse as XML
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(templateContent);

                // Check for required elements
                XmlNodeList reportNodes = doc.GetElementsByTagName("Report");
                if (reportNodes.Count == 0)
                {
                    Logger.LogWarning("Template validation failed: Missing Report element");
                    return false;
                }

                Logger.Log("Template validation successful");
                return true;
            }
            catch (XmlException ex)
            {
                Logger.LogWarning($"Template validation failed: Invalid XML - {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error validating template: {ex.Message}");
                return false;
            }
        }
    }
}