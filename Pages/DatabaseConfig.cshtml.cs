using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using ReportVisualizer.DataAccessLayer.Models;
using ReportVisualizer.Security;
using System.Text.Json;
using System.Text.Json.Nodes;


namespace ReportVisualizer.Pages
{
    [DevOnly]
    public class DatabaseConfigPageModel : PageModel
    {
        private readonly IConfiguration _configuration;

        [BindProperty]
        public DatabaseConfigModel Config { get; set; } = new DatabaseConfigModel();

        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsDbConnected { get; set; }
        public string GeneratedConnectionString { get; set; } = string.Empty;

        public List<string> AvailableTables { get; set; } = new List<string>();
        public List<string> AvailableColumns { get; set; } = new List<string>();

        public DatabaseConfigPageModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            try
            {
                IsDbConnected = DataAccessLayer.DatabaseConfig.DatabaseConnection.Instance.IsConnected;
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

                // Update appsettings.json
                var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                if (System.IO.File.Exists(appSettingsPath))
                {
                    var jsonString = System.IO.File.ReadAllText(appSettingsPath);
                    var jsonDoc = JsonDocument.Parse(jsonString);
                    var root = JsonNode.Parse(jsonString);

                    if (root != null)
                    {
                        var connectionStringsNode = root["ConnectionStrings"];
                        if (connectionStringsNode == null)
                        {
                            connectionStringsNode = new JsonObject();
                            root["ConnectionStrings"] = connectionStringsNode;
                        }
                        connectionStringsNode["DefaultConnection"] = GeneratedConnectionString;

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        System.IO.File.WriteAllText(appSettingsPath, root.ToJsonString(options));
                        Message = "appsettings.json updated successfully.";
                    }
                    else
                    {
                        ErrorMessage = "Failed to parse appsettings.json.";
                    }
                }
                else
                {
                    ErrorMessage = "appsettings.json not found.";
                }

                ReportVisualizer.Utilities.GlobalConnectionHandler.ConnectionString = GeneratedConnectionString;
                ReportVisualizer.Utilities.GlobalConnectionHandler.Initialize();
                IsDbConnected = ReportVisualizer.DataAccessLayer.DatabaseConfig.DatabaseConnection.Instance.IsConnected;

                if (IsDbConnected)
                {
                    Message += " Database configuration saved and connected successfully.";
                }
                else
                {
                    ErrorMessage += " Database configuration saved, but connection failed. Please check details.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving database configuration: {ex.Message}";
            }
            return Page();
        }

        public JsonResult OnGetTables()
        {
            var tables = new List<string>();
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var schema = conn.GetSchema("Tables");
                    foreach (System.Data.DataRow row in schema.Rows)
                    {
                        tables.Add(row["TABLE_NAME"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
            return new JsonResult(tables);
        }

        public JsonResult OnGetColumns(string tableName)
        {
            var columns = new List<string>();
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var schema = conn.GetSchema("Columns", new string[] { null, null, tableName, null });
                    foreach (System.Data.DataRow row in schema.Rows)
                    {
                        columns.Add(row["COLUMN_NAME"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
            return new JsonResult(columns);
        }

        public class QueryData
        {
            public string TableName { get; set; }
            public List<string> Columns { get; set; }
            public string DateTimeColumn { get; set; }
            public string ProcedureName { get; set; }
        }

        public class ColumnInfo
        {
            public string ColumnName { get; set; }
            public string DataType { get; set; }
        }

        public JsonResult OnGetColumnDataTypes(string tableName)
        {
            var columnInfos = new List<ColumnInfo>();
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var schema = conn.GetSchema("Columns", new string[] { null, null, tableName, null });
                    foreach (System.Data.DataRow row in schema.Rows)
                    {
                        columnInfos.Add(new ColumnInfo
                        {
                            ColumnName = row["COLUMN_NAME"].ToString(),
                            DataType = row["DATA_TYPE"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
            return new JsonResult(columnInfos);
        }

        public JsonResult OnPostExecuteQuery([FromBody] QueryData data)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    var selectColumns = string.Join(", ", data.Columns.Select(c => $"[{c}]"));

                    string createProcedureSql = $@"
                        CREATE OR ALTER PROCEDURE {data.ProcedureName}
                        (
                            @StartDateTime  DATETIME = '2026-01-01 00:00:00',
                            @EndDateTime    DATETIME = '2026-01-31 23:59:59',
                            @IntervalType   NVARCHAR(10) = 'MINUTE',
                            @IntervalValue  INT = 2
                        )
                        AS
                        BEGIN
                            SET NOCOUNT ON;

                            ------------------------------------------------------------
                            -- Configuration Variables (Edit these for your table)
                            ------------------------------------------------------------
                            DECLARE @SourceTable      NVARCHAR(300) = '{data.TableName}';
                            DECLARE @DateTimeColumn   NVARCHAR(128) = '{data.DateTimeColumn}';
                            DECLARE @SelectColumns    NVARCHAR(MAX) = '{selectColumns}';
                            DECLARE @AdditionalFilter NVARCHAR(MAX) = '';

                            ------------------------------------------------------------
                            -- Validation
                            ------------------------------------------------------------
                            IF @IntervalValue <= 0
                            BEGIN
                                RAISERROR('IntervalValue must be greater than 0', 16, 1);
                                RETURN;
                            END

                            IF @StartDateTime > @EndDateTime
                            BEGIN
                                RAISERROR('StartDateTime cannot be greater than EndDateTime', 16, 1);
                                RETURN;
                            END

                            ------------------------------------------------------------
                            -- Calculate Interval in Minutes
                            ------------------------------------------------------------
                            DECLARE @IntervalMinutes INT;
                            IF UPPER(@IntervalType) = 'MINUTE'
                                SET @IntervalMinutes = @IntervalValue;
                            ELSE IF UPPER(@IntervalType) = 'HOUR'
                                SET @IntervalMinutes = @IntervalValue * 60;
                            ELSE IF UPPER(@IntervalType) = 'DAY'
                                SET @IntervalMinutes = @IntervalValue * 24 * 60;
                            ELSE IF UPPER(@IntervalType) = 'WEEK'
                                SET @IntervalMinutes = @IntervalValue * 7 * 24 * 60;
                            ELSE
                            BEGIN
                                RAISERROR('Invalid IntervalType. Allowed: MINUTE, HOUR, DAY, WEEK', 16, 1);
                                RETURN;
                            END

                            ------------------------------------------------------------
                            -- Generate Time Buckets (Slots) - END Based
                            ------------------------------------------------------------
                            DROP TABLE IF EXISTS #TimeBuckets;
                            CREATE TABLE #TimeBuckets
                            (
                                SlotEnd   DATETIME NOT NULL,
                                SlotStart DATETIME NOT NULL
                            );

                            CREATE CLUSTERED INDEX IX_TimeBuckets_SlotEnd ON #TimeBuckets (SlotEnd);

                            DECLARE @BucketCount INT;
                            SET @BucketCount = CEILING(DATEDIFF(MINUTE, @StartDateTime, @EndDateTime) * 1.0 / @IntervalMinutes);

                            ;WITH Tally AS (
                                SELECT TOP (@BucketCount)
                                    ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RN
                                FROM sys.all_objects A
                                CROSS JOIN sys.all_objects B
                            )
                            INSERT INTO #TimeBuckets (SlotEnd, SlotStart)
                            SELECT
                                DATEADD(MINUTE, RN * @IntervalMinutes, @StartDateTime) AS SlotEnd,
                                DATEADD(MINUTE, (RN - 1) * @IntervalMinutes, @StartDateTime) AS SlotStart
                            FROM Tally;

                            ------------------------------------------------------------
                            -- Build Dynamic SQL
                            -- KEY FIX: Only return data if timestamp MATCHES bucket end time
                            ------------------------------------------------------------
                            DECLARE @SQL NVARCHAR(MAX);

                            SET @SQL = N'
                            SELECT
                                Sub.SlotEnd AS IntervalStart,
                                ' + @SelectColumns + N'
                            FROM (
                                SELECT
                                    T.SlotEnd,
                                    ' + @SelectColumns + N',
                                    S.' + QUOTENAME(@DateTimeColumn) + N',
                                    ROW_NUMBER() OVER (
                                        PARTITION BY T.SlotEnd
                                        ORDER BY S.' + QUOTENAME(@DateTimeColumn) + N' DESC
                                    ) AS RN,
                                    CASE
                                        WHEN S.' + QUOTENAME(@DateTimeColumn) + N' = T.SlotEnd THEN 1
                                        ELSE 0
                                    END AS IsExactMatch
                                FROM #TimeBuckets T
                                LEFT JOIN ' + QUOTENAME(@SourceTable) + N' S WITH (NOLOCK)
                                    ON S.' + QUOTENAME(@DateTimeColumn) + N' > T.SlotStart
                                    AND S.' + QUOTENAME(@DateTimeColumn) + N' <= T.SlotEnd
                                    ' + CASE WHEN ISNULL(@AdditionalFilter,'') <> '' THEN N' AND ' + @AdditionalFilter ELSE N'' END + N'
                            ) Sub
                            WHERE Sub.RN = 1
                              AND Sub.IsExactMatch = 1
                            ORDER BY Sub.SlotEnd;';

                            ------------------------------------------------------------
                            -- Execute
                            ------------------------------------------------------------
                            -- PRINT @SQL;  -- For debugging
                            EXEC sp_executesql @SQL;

                        END
                    ";

                    using (var cmd = new SqlCommand(createProcedureSql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                return new JsonResult(new { success = true, message = "Stored procedure 'dbo.usp_GenericTimeSeriesInterval' created/altered successfully." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
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