using System.Text;

namespace uTPro.Feature.AuditLog.Services;

internal static class CsvHelper
{
    public static string ToCsv<T>(IEnumerable<T> items, string[] headers, Func<T, string[]> rowSelector)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(Escape)));

        foreach (var item in items)
            sb.AppendLine(string.Join(",", rowSelector(item).Select(Escape)));

        return sb.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return $"\"{value}\"";
    }
}
