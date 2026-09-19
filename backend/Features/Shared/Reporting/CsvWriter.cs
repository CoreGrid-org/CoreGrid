using System.Text;

namespace CoreGrid.Api.Features.Shared.Reporting;

// The escaping/row-building logic duplicated identically in
// AuditReportService and CampaignReportService. A caller builds a report's
// CSV by writing rows in order (blank rows are just WriteRow() with no
// fields, matching both existing reports' section-break style) and reads
// the result back with GetBytes().
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
