using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ReportVisualizer.Security
{
    public class ScadaLoginOptions
    {
        public bool Enable { get; set; }
        public string DataTableName { get; set; } = "User_list";
        public ScadaLoginEncryptionOptions Encryption { get; set; } = new ScadaLoginEncryptionOptions();
    }

    public class ScadaLoginEncryptionOptions
    {
        public bool Enable { get; set; }
        public int Key { get; set; } = 3;
    }

    public class ScadaLoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
    }

    public class ScadaLoginService
    {
        private readonly IConfiguration _configuration;
        private readonly ScadaLoginOptions _options;

        public ScadaLoginService(IConfiguration configuration)
        {
            _configuration = configuration;
            _options = new ScadaLoginOptions();
            configuration.GetSection("ScadaLogin").Bind(_options);
            if (_options.Encryption == null) _options.Encryption = new ScadaLoginEncryptionOptions();
            if (string.IsNullOrWhiteSpace(_options.DataTableName)) _options.DataTableName = "User_list";
        }

        public ScadaLoginOptions Options => _options;

        public string SessionUserKey => "ScadaLogin:User";
        public string SessionFullNameKey => "ScadaLogin:FullName";
        public string SessionTokenKey => "ScadaLogin:Token";

        public string EncryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return password;
            if (!_options.Encryption.Enable) return password;
            int shift = _options.Encryption.Key;
            var chars = new char[password.Length];
            for (int i = 0; i < password.Length; i++)
            {
                chars[i] = (char)(password[i] + shift);
            }
            return new string(chars);
        }

        public string DecryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return password;
            if (!_options.Encryption.Enable) return password;
            int shift = _options.Encryption.Key;
            var chars = new char[password.Length];
            for (int i = 0; i < password.Length; i++)
            {
                chars[i] = (char)(password[i] - shift);
            }
            return new string(chars);
        }

        public void EnsureLoginStatusTable()
        {
            if (!_options.Enable) return;
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(connectionString)) return;

                using var conn = new SqlConnection(connectionString);
                conn.Open();

                var tableName = "report_login_status";
                var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = @"
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @tableName;
                ";
                checkCmd.Parameters.Add(new SqlParameter("@tableName", SqlDbType.NVarChar, 128) { Value = tableName });
                var exists = checkCmd.ExecuteScalar() != null;
                if (exists) return;

                var createCmd = conn.CreateCommand();
                createCmd.CommandText = $@"
                    CREATE TABLE [dbo].[{tableName}] (
                        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY NOT NULL,
                        [Username] NVARCHAR(255) NOT NULL,
                        [LoginToken] NVARCHAR(512) NOT NULL,
                        [MachineName] NVARCHAR(255) NULL,
                        [IpAddress] NVARCHAR(128) NULL,
                        [LoggedInAt] DATETIME2 NOT NULL DEFAULT(GETDATE())
                    );
                    CREATE UNIQUE INDEX [UX_{tableName}_LoginToken] ON [dbo].[{tableName}]([LoginToken]);
                    CREATE INDEX [IX_{tableName}_Username] ON [dbo].[{tableName}]([Username]);
                ";
                createCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring report_login_status table exists: {ex.Message}");
            }
        }

        public ScadaLoginResult ValidateCredentials(string username, string password)
        {
            var result = new ScadaLoginResult { Message = "Invalid username or password." };
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return result;
            }

            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    result.Message = "Database is not configured.";
                    return result;
                }

                string tableName = _options.DataTableName ?? "User_list";
                using var conn = new SqlConnection(connectionString);
                conn.Open();

                var schemaCmd = conn.CreateCommand();
                schemaCmd.CommandText = @"
                    SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @tableName;
                ";
                schemaCmd.Parameters.Add(new SqlParameter("@tableName", SqlDbType.NVarChar, 128) { Value = tableName });
                var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var rdr = schemaCmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        columns.Add(Convert.ToString(rdr["COLUMN_NAME"]));
                    }
                }

                if (!columns.Contains("Username"))
                {
                    result.Message = $"User table '{tableName}' does not contain a Username column.";
                    return result;
                }

                List<string> passwordCandidates = new List<string>();
                if (columns.Contains("Password")) passwordCandidates.Add("Password");

                if (passwordCandidates.Count == 0)
                {
                    result.Message = $"User table '{tableName}' does not contain a Password column.";
                    return result;
                }

                string fullNameSelect = columns.Contains("Userfullname") ? ", [Userfullname] " : ", NULL AS [Userfullname] ";
                string passwordSelect = string.Join(", ", passwordCandidates.ConvertAll(p => $"[{p}]"));
                string whereClause = $"WHERE LOWER([Username]) = LOWER(@username)";

                var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT TOP 1 [Username] {fullNameSelect}, {passwordSelect} FROM [dbo].[{SqlEscapeIdentifier(tableName)}] {whereClause};";
                cmd.Parameters.Add(new SqlParameter("@username", SqlDbType.NVarChar, 255) { Value = username });

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    return result;
                }

                var dbUsername = Convert.ToString(reader["Username"]);
                string? fullName = null;
                if (columns.Contains("Userfullname"))
                {
                    fullName = reader["Userfullname"] == DBNull.Value ? null : Convert.ToString(reader["Userfullname"]);
                }

                bool encryptionEnabled = _options.Encryption.Enable;
                string encryptedInput = encryptionEnabled ? EncryptPassword(password) : password;
                string decryptedDbFallback = null;
                foreach (var p in passwordCandidates)
                {
                    var value = reader[p];
                    if (value == DBNull.Value || value == null) continue;
                    var dbPassword = Convert.ToString(value);
                    if (!encryptionEnabled)
                    {
                        if (string.Equals(dbPassword, password, StringComparison.Ordinal))
                        {
                            result.Success = true;
                            result.Username = dbUsername;
                            result.FullName = string.IsNullOrWhiteSpace(fullName) ? dbUsername : fullName;
                            result.Message = "Login successful.";
                            return result;
                        }
                    }
                    else
                    {
                        if (string.Equals(dbPassword, encryptedInput, StringComparison.Ordinal))
                        {
                            result.Success = true;
                            result.Username = dbUsername;
                            result.FullName = string.IsNullOrWhiteSpace(fullName) ? dbUsername : fullName;
                            result.Message = "Login successful.";
                            return result;
                        }
                        if (string.IsNullOrEmpty(decryptedDbFallback))
                        {
                            decryptedDbFallback = DecryptPassword(dbPassword);
                        }
                    }
                }

                if (encryptionEnabled &&
                    !string.IsNullOrEmpty(decryptedDbFallback) &&
                    string.Equals(decryptedDbFallback, password, StringComparison.Ordinal))
                {
                    result.Success = true;
                    result.Username = dbUsername;
                    result.FullName = string.IsNullOrWhiteSpace(fullName) ? dbUsername : fullName;
                    result.Message = "Login successful.";
                    return result;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Message = $"Login error: {ex.Message}";
                return result;
            }
        }

        public string CreateLoginSession(HttpContext context, string username, string fullName)
        {
            var token = Guid.NewGuid().ToString("N") + "-" + DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString("X");
            context.Session.SetString(SessionUserKey, username);
            if (!string.IsNullOrWhiteSpace(fullName))
                context.Session.SetString(SessionFullNameKey, fullName);
            context.Session.SetString(SessionTokenKey, token);
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    using var conn = new SqlConnection(connectionString);
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO [dbo].[report_login_status] ([Username], [LoginToken], [MachineName], [IpAddress])
                        VALUES (@username, @token, @machine, @ip);
                    ";
                    cmd.Parameters.Add(new SqlParameter("@username", SqlDbType.NVarChar, 255) { Value = username });
                    cmd.Parameters.Add(new SqlParameter("@token", SqlDbType.NVarChar, 512) { Value = token });
                    cmd.Parameters.Add(new SqlParameter("@machine", SqlDbType.NVarChar, 255) { Value = (object?)Environment.MachineName ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@ip", SqlDbType.NVarChar, 128) { Value = (object?)context.Connection.RemoteIpAddress?.ToString() ?? DBNull.Value });
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting login status: {ex.Message}");
            }
            return token;
        }

        public void ClearLoginSession(HttpContext context)
        {
            var token = context.Session.GetString(SessionTokenKey);
            context.Session.Remove(SessionUserKey);
            context.Session.Remove(SessionFullNameKey);
            context.Session.Remove(SessionTokenKey);

            if (string.IsNullOrWhiteSpace(token)) return;
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(connectionString)) return;
                using var conn = new SqlConnection(connectionString);
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"DELETE FROM [dbo].[report_login_status] WHERE [LoginToken] = @token;";
                cmd.Parameters.Add(new SqlParameter("@token", SqlDbType.NVarChar, 512) { Value = token });
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting login status: {ex.Message}");
            }
        }

        public bool IsLoggedIn(HttpContext context)
        {
            if (!_options.Enable) return true;
            var user = context.Session.GetString(SessionUserKey);
            var token = context.Session.GetString(SessionTokenKey);
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(token)) return false;
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(connectionString)) return true;
                using var conn = new SqlConnection(connectionString);
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT 1 FROM [dbo].[report_login_status] WHERE [LoginToken] = @token AND [Username] = @username;";
                cmd.Parameters.Add(new SqlParameter("@token", SqlDbType.NVarChar, 512) { Value = token });
                cmd.Parameters.Add(new SqlParameter("@username", SqlDbType.NVarChar, 255) { Value = user });
                return cmd.ExecuteScalar() != null;
            }
            catch
            {
                return true;
            }
        }

        public string? GetCurrentUser(HttpContext context) => context.Session.GetString(SessionUserKey);
        public string? GetCurrentFullName(HttpContext context) => context.Session.GetString(SessionFullNameKey);

        private static string SqlEscapeIdentifier(string name)
        {
            return name.Replace("]", "]]");
        }
    }
}
