using System.Text;

namespace CoreGrid.Api.Features.Shared.Reporting;

// Provides reusable CSV writing utilities.
public class CsvWriter
{
    private readonly StringBuilder _sb = new();

    public void WriteRow(params object?[] fields) =>
        _sb.AppendLine(string.Join(",", fields.Select(Escape)));

    public void WriteBlankRow() => _sb.AppendLine();

    public byte[] GetBytes() => Encoding.UTF8.GetBytes(_sb.ToString());

    private static string Escape(object? value)
    {
        var text = value?.ToString() ?? "";
        return text.Contains(',') || text.Contains('"') || text.Contains('\n')
            ? $"\"{text.Replace("\"", "\"\"")}\""
            : text;
    }
}
