using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace ReportVisualizer.ReportViewer
{
    public class DataTableColumnDto
    {
        public string Name { get; set; }
        public string DataType { get; set; }
    }

    public class DataTableDto
    {
        public string Name { get; set; }
        public List<DataTableColumnDto> Columns { get; set; } = new List<DataTableColumnDto>();
        public List<List<object>> Rows { get; set; } = new List<List<object>>();
    }

    public class ReportDataSourceSnapshot
    {
        public string Name { get; set; }
        public DataTableDto Table { get; set; }
    }

    public class RenderedReportSnapshot
    {
        public string ReportName { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public List<ReportDataSourceSnapshot> DataSources { get; set; } = new List<ReportDataSourceSnapshot>();
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public static class DataTableDtoMapper
    {
        public static object UnwrapJsonElement(object value)
        {
            if (value == null) return null;
            if (!(value is JsonElement je)) return value;
            switch (je.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Number:
                    if (je.TryGetInt32(out var i)) return i;
                    if (je.TryGetInt64(out var l)) return l;
                    if (je.TryGetDecimal(out var dec)) return dec;
                    if (je.TryGetDouble(out var d)) return d;
                    return je.GetRawText();
                case JsonValueKind.String:
                    if (je.TryGetDateTime(out var dt)) return dt;
                    if (je.TryGetDateTimeOffset(out var dto)) return dto;
                    if (je.TryGetGuid(out var g)) return g;
                    return je.GetString();
                default:
                    return je.GetRawText();
            }
        }

        public static Dictionary<string, object> NormalizeParameterDictionary(IDictionary<string, object> parameters)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (parameters == null) return result;
            foreach (var kv in parameters)
            {
                var v = UnwrapJsonElement(kv.Value);
                result[kv.Key] = v;
            }
            return result;
        }

        public static DataTableDto ToDto(DataTable table)
        {
            if (table == null) return new DataTableDto();
            var dto = new DataTableDto
            {
                Name = table.TableName
            };
            foreach (DataColumn col in table.Columns)
            {
                dto.Columns.Add(new DataTableColumnDto
                {
                    Name = col.ColumnName,
                    DataType = col.DataType.AssemblyQualifiedName
                });
            }
            foreach (DataRow row in table.Rows)
            {
                var values = new List<object>();
                foreach (DataColumn col in table.Columns)
                {
                    var v = row[col];
                    if (v == null || v == DBNull.Value)
                    {
                        values.Add(null);
                    }
                    else if (col.DataType == typeof(DateTime))
                    {
                        values.Add(((DateTime)v).ToString("o"));
                    }
                    else if (col.DataType == typeof(DateTimeOffset))
                    {
                        values.Add(((DateTimeOffset)v).ToString("o"));
                    }
                    else if (col.DataType == typeof(TimeSpan))
                    {
                        values.Add(((TimeSpan)v).ToString("c"));
                    }
                    else if (col.DataType == typeof(byte[]))
                    {
                        values.Add(Convert.ToBase64String((byte[])v));
                    }
                    else
                    {
                        values.Add(v);
                    }
                }
                dto.Rows.Add(values);
            }
            return dto;
        }

        public static DataTable ToDataTable(DataTableDto dto)
        {
            var dt = new DataTable();
            if (dto == null) return dt;
            if (!string.IsNullOrWhiteSpace(dto.Name)) dt.TableName = dto.Name;
            var typeMap = new Dictionary<int, Type>();
            for (int i = 0; i < dto.Columns.Count; i++)
            {
                var col = dto.Columns[i];
                Type t = null;
                try
                {
                    if (!string.IsNullOrEmpty(col.DataType))
                    {
                        t = Type.GetType(col.DataType, throwOnError: false, ignoreCase: true);
                        if (t == null)
                        {
                            var comma = col.DataType.IndexOf(',');
                            var shortName = comma > 0 ? col.DataType.Substring(0, comma).Trim() : col.DataType;
                            t = Type.GetType(shortName, throwOnError: false, ignoreCase: true);
                        }
                    }
                }
                catch { t = null; }
                if (t == null) t = typeof(string);
                typeMap[i] = t;
                dt.Columns.Add(col.Name, t);
            }
            foreach (var row in dto.Rows ?? new List<List<object>>())
            {
                var dr = dt.NewRow();
                for (int i = 0; i < dto.Columns.Count && i < (row?.Count ?? 0); i++)
                {
                    var rawValue = row[i];
                    var value = UnwrapJsonElement(rawValue);
                    var t = typeMap[i];
                    try
                    {
                        if (value == null)
                        {
                            dr[i] = DBNull.Value;
                        }
                        else if (t == typeof(DateTime) && value is DateTime dtVal)
                        {
                            dr[i] = dtVal;
                        }
                        else if (t == typeof(DateTimeOffset) && value is DateTimeOffset dtoVal)
                        {
                            dr[i] = dtoVal;
                        }
                        else if (t == typeof(TimeSpan))
                        {
                            if (value is TimeSpan ts) dr[i] = ts;
                            else if (TimeSpan.TryParse(Convert.ToString(value), out var ts2)) dr[i] = ts2;
                            else dr[i] = DBNull.Value;
                        }
                        else if (t == typeof(byte[]))
                        {
                            if (value is byte[] b) dr[i] = b;
                            else if (value is string s) dr[i] = Convert.FromBase64String(s);
                            else dr[i] = DBNull.Value;
                        }
                        else if (t == typeof(string))
                        {
                            dr[i] = value?.ToString() ?? (object)DBNull.Value;
                        }
                        else if (value.GetType() == t || t.IsInstanceOfType(value))
                        {
                            dr[i] = value;
                        }
                        else
                        {
                            dr[i] = Convert.ChangeType(value, t);
                        }
                    }
                    catch
                    {
                        dr[i] = DBNull.Value;
                    }
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }
    }

    public static class ReportSnapshotSerializer
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        public static string SerializeSnapshot(RenderedReportSnapshot snapshot)
        {
            return JsonSerializer.Serialize(snapshot, Options);
        }

        public static RenderedReportSnapshot DeserializeSnapshot(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<RenderedReportSnapshot>(json, Options);
        }
    }

    public static class ReportSnapshotSessionStore
    {
        private const string LastSnapshotKey = "LastRenderedReportSnapshot";
        public static void Store(ISession session, RenderedReportSnapshot snapshot)
        {
            if (session == null || snapshot == null) return;
            session.SetString(LastSnapshotKey, ReportSnapshotSerializer.SerializeSnapshot(snapshot));
        }
        public static RenderedReportSnapshot Get(ISession session)
        {
            if (session == null) return null;
            var json = session.GetString(LastSnapshotKey);
            return ReportSnapshotSerializer.DeserializeSnapshot(json);
        }
        public static void Clear(ISession session)
        {
            if (session == null) return;
            session.Remove(LastSnapshotKey);
        }
    }
}
