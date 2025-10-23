using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using ReportVisualizer.DataAccessLayer.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ReportVisualizer.Pages
{
    public class DatabaseConfigPageModel : PageModel
    {
        private readonly IConfiguration _configuration;

        [BindProperty]
        public DatabaseConfigModel Config { get; set; } = new DatabaseConfigModel();

        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public string GeneratedConnectionString { get; set; } = string.Empty;

        public DatabaseConfigPageModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            try
            {
                var current = _configuration.GetConnectionString("DefaultConnection");
                if (!string.IsNullOrWhiteSpace(current))
                {
                    ParseConnectionString(current, Config);
                    GeneratedConnectionString = current;
                }
                else
                {
                    Message = "No existing connection string found. Please enter details.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load current configuration: {ex.Message}";
            }
        }

        public IActionResult OnPostTest()
        {
            try
            {
                GeneratedConnectionString = Config.GenerateConnectionString();
                using (var conn = new SqlConnection(GeneratedConnectionString))
                {
                    conn.Open();
                    conn.Close();
                }
                Message = "Connection test succeeded.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Connection test failed: {ex.Message}";
            }
            return Page();
        }

        public IActionResult OnPostSave()
        {
            try
            {
                GeneratedConnectionString = Config.GenerateConnectionString();

                var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                if (!System.IO.File.Exists(appSettingsPath))
                {
                    throw new FileNotFoundException($"appsettings.json not found at {appSettingsPath}");
                }

                var jsonText = System.IO.File.ReadAllText(appSettingsPath);
                var json = JsonNode.Parse(jsonText) as JsonObject;
                if (json == null)
                {
                    throw new InvalidOperationException("Invalid appsettings.json format.");
                }

                JsonObject connectionStrings;
                if (json.ContainsKey("ConnectionStrings") && json["ConnectionStrings"] is JsonObject existing)
                {
                    connectionStrings = existing;
                }
                else
                {
                    connectionStrings = new JsonObject();
                    json["ConnectionStrings"] = connectionStrings;
                }

                connectionStrings["DefaultConnection"] = GeneratedConnectionString;

                var updated = json.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(appSettingsPath, updated);

                Message = "Configuration saved. Restart the application to apply changes.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to save configuration: {ex.Message}";
            }
            return Page();
        }

        private static void ParseConnectionString(string connectionString, DatabaseConfigModel model)
        {
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2)
                {
                    dict[kv[0].Trim()] = kv[1].Trim();
                }
            }

            // Server
            if (dict.TryGetValue("Server", out var server) || dict.TryGetValue("Data Source", out server))
            {
                model.ServerName = server;
            }

            // Database
            if (dict.TryGetValue("Database", out var db) || dict.TryGetValue("Initial Catalog", out db))
            {
                model.DatabaseName = db;
            }

            // Integrated Security
            if (dict.TryGetValue("Integrated Security", out var integrated))
            {
                model.UseIntegratedSecurity = integrated.Equals("True", StringComparison.OrdinalIgnoreCase) || integrated.Equals("SSPI", StringComparison.OrdinalIgnoreCase);
            }

            // Username
            if (dict.TryGetValue("User Id", out var user) || dict.TryGetValue("UID", out user))
            {
                model.Username = user;
            }

            // Password
            if (dict.TryGetValue("Password", out var pwd) || dict.TryGetValue("PWD", out pwd))
            {
                model.Password = pwd;
            }

            // Timeout
            if (dict.TryGetValue("Connect Timeout", out var timeoutStr) && int.TryParse(timeoutStr, out var timeout))
            {
                model.ConnectionTimeout = timeout;
            }

            // Trust Server Certificate
            if (dict.TryGetValue("TrustServerCertificate", out var trustServerCertificateStr))
            {
                model.TrustServerCertificate = trustServerCertificateStr.Equals("True", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}