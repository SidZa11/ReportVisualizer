using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace ReportVisualizer.ReportViewer.ReportDataExecution
{
    public class SqlDatasetExecutor
    {
        public async Task<DataTable> ExecuteQueryAsync(string connectionString, string commandText, string commandType, Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable();

            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand(commandText, connection))
                {
                    // Add parameters to the SqlCommand
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            object value = param.Value;
                            Console.WriteLine($"Parameter: {param.Key}, Value: {value?.ToString() ?? "null"}");

                            // Handle ASP.NET form values (StringValues)
                            if (value is Microsoft.Extensions.Primitives.StringValues sv)
                            {
                                if (sv.Count == 0)
                                    value = DBNull.Value;
                                else
                                    value = sv[0].ToString(); // first value
                            }

                            // Empty string -> NULL
                            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                value = DBNull.Value;

                            // Detect DateTime (important for BETWEEN)
                            if (value != DBNull.Value && DateTime.TryParse(value.ToString(), out DateTime dt))
                            {
                                command.Parameters.Add(new SqlParameter("@" + param.Key, SqlDbType.DateTime)
                                {
                                    Value = dt
                                });
                            }
                            else
                            {
                                command.Parameters.Add(new SqlParameter("@" + param.Key, value));
                            }
                        }


                    }

                    string actualCommandType = commandType ?? "Text"; // Default to Text if null
                    if (actualCommandType.ToLower() == "storedprocedure")
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }
                    else // Default to Text
                    {
                        command.CommandType = CommandType.Text;
                    }

                    await connection.OpenAsync();
                    Console.WriteLine("Database connection opened successfully.");
                    Console.WriteLine($"Executing SQL Query: {commandText}");

                    using (var adapter = new SqlDataAdapter(command))
                    {
                        Console.WriteLine("Attempting to fill data table...");
                        adapter.Fill(dataTable);
                        Console.WriteLine($"Data table filled with {dataTable.Rows.Count} rows.");
                    }
                }
            }
            Console.WriteLine("Query execution completed.");
            return dataTable;
        }
    }
}