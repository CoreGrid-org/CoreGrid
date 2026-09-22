using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CoreGrid.Api.Features.Shared.Reporting;

// Provides reusable PDF components for reports.
public static class PdfComponents
{
    public static void StatBox(IContainer container, string label, string value)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
        {
            c.Item().Text(label).FontColor(Colors.Grey.Darken1);
            c.Item().Text(value).FontSize(16).Bold();
        });
    }

    public static void CountTable(IContainer container, string title, IReadOnlyCollection<(string Label, int Count)> counts)
    {
        container.Column(column =>
        {
            column.Item().Text(title).Bold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3);
                    c.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Text("Label").Bold();
                    header.Cell().Text("Count").Bold();
                });

                if (counts.Count == 0)
                {
                    table.Cell().ColumnSpan(2).Text("None").FontColor(Colors.Grey.Darken1);
                }

                foreach (var c in counts)
                {
                    table.Cell().Text(c.Label);
                    table.Cell().Text(c.Count.ToString());
                }
            });
        });
    }
}
