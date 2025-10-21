using System;
using System.Data;

namespace ReportVisualizer.DataAccessLayer.Models
{
    /// <summary>
    /// Model for report data
    /// </summary>
    public class ReportDataModel
    {
        /// <summary>
        /// Gets or sets the report ID
        /// </summary>
        public Guid ReportId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the report name
        /// </summary>
        public string ReportName { get; set; }

        /// <summary>
        /// Gets or sets the report description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the template ID used for this report
        /// </summary>
        public string TemplateId { get; set; }

        /// <summary>
        /// Gets or sets the database configuration used for this report
        /// </summary>
        public DatabaseConfigModel DatabaseConfig { get; set; }

        /// <summary>
        /// Gets or sets the SQL query used to generate the report
        /// </summary>
        public string SqlQuery { get; set; }

        /// <summary>
        /// Gets or sets the report parameters
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// Gets or sets the report data
        /// </summary>
        public DataTable ReportData { get; set; }

        /// <summary>
        /// Gets or sets the creation date
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the last modified date
        /// </summary>
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the user who created the report
        /// </summary>
        public string CreatedBy { get; set; }
    }
}