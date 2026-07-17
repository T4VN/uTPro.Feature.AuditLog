using System.Text;

namespace uTPro.Feature.AuditLog.Services;

internal static class CsvHelper
{
    public static string ToCsv<T>(IEnumerable<T> items, string[] headers, Func<T, string[]> rowSelector)
    {
        // Pre-size the buffer to avoid repeated doubling reallocations while building large
        // exports (each growth copies the whole char[] and adds GC pressure). When the row
        // count is cheaply known we estimate from it; otherwise we start at a sane default.
        // Capacity affects only allocation strategy, never the produced text.
        const int estimatedRowWidth = 256;
        var initialCapacity = items is ICollection<T> collection
            ? (collection.Count + 1) * estimatedRowWidth
            : 64 * 1024;

        var sb = new StringBuilder(initialCapacity);
        sb.AppendLine(string.Join(",", headers.Select(Escape)));

        foreach (var item in items)
            sb.AppendLine(string.Join(",", rowSelector(item).Select(Escape)));

        return sb.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";

        // Mitigate CSV/formula injection: spreadsheet applications (Excel, Sheets, ...) will
        // evaluate a cell as a formula when its value begins with =, +, -, @, TAB or CR.
        // Prefix such values with a single quote so they are treated as literal text.
        // The check ignores leading whitespace, and runs BEFORE the structural CSV
        // quote-wrapping/doubling below so the guard character is preserved inside the field.
        var trimmed = value.TrimStart();
        if (trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = "'" + value;

        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return $"\"{value}\"";
    }
}
