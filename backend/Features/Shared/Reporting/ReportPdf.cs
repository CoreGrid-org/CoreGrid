using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CoreGrid.Api.Features.Shared.Reporting;

// House style for CoreGrid PDF reports: the same brand colours as the web
// app, a header band, KPI tiles, striped tables, status pills, a sign-off
// block and a "page X of Y" footer. Reports compose these instead of each
// styling QuestPDF from scratch.
public static class ReportPdf
{
    public const string Accent = "#406AAF";
    public const string AccentDark = "#2D5190";
    public const string AccentLight = "#EDF2FA";
    public const string Text = "#161616";
    public const string Muted = "#525252";
    public const string Subtle = "#8D8D8D";
    public const string Border = "#E0E0E0";
    public const string Zebra = "#F7F9FC";

    public const string Green = "#198038";
    public const string Red = "#DA1E28";
    public const string Amber = "#8E6A00";
    public const string Purple = "#6929C4";
    public const string Grey = "#6F6F6F";

    private static readonly Dictionary<string, string> PillBackgrounds = new()
    {
        [Green] = "#DEFBE6",
        [Red] = "#FFF1F1",
        [Amber] = "#FCF4D6",
        [Purple] = "#F6F2FF",
        [Grey] = "#F4F4F4",
        [Accent] = AccentLight,
    };

    public static void ConfigurePage(PageDescriptor page, PageSize size)
    {
        page.Size(size);
        page.MarginHorizontal(32);
        page.MarginVertical(28);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(Text));
    }

    public static void Header(IContainer container, string organization, string title, string subtitle)
    {
        container.PaddingBottom(10).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(organization.ToUpperInvariant()).FontSize(7.5f).SemiBold().FontColor(Accent).LetterSpacing(0.05f);
                    c.Item().PaddingTop(2).Text(title).FontSize(17).Bold();
                    c.Item().Text(subtitle).FontSize(9.5f).FontColor(Muted);
                });
                row.ConstantItem(90).AlignRight().AlignTop().Column(c =>
                {
                    c.Item().AlignRight().Text("CoreGrid").FontSize(13).Bold().FontColor(Accent);
                    c.Item().AlignRight().Text("Asset lifecycle management").FontSize(6.5f).FontColor(Subtle);
                });
            });
            column.Item().PaddingTop(8).Height(2).Background(Accent);
        });
    }

    public static void Footer(IContainer container, string note)
    {
        container.BorderTop(0.5f).BorderColor(Border).PaddingTop(5).Row(row =>
        {
            row.RelativeItem().Text(note).FontSize(7).FontColor(Subtle);
            row.ConstantItem(80).AlignRight().Text(x =>
            {
                x.DefaultTextStyle(s => s.FontSize(7).FontColor(Subtle));
                x.Span("Page ");
                x.CurrentPageNumber();
                x.Span(" of ");
                x.TotalPages();
            });
        });
    }

    /// <summary>Label/value pairs laid out in a grid, e.g. period, scope, status.</summary>
    public static void MetaGrid(IContainer container, IReadOnlyList<(string Label, string Value)> items, int columns = 3)
    {
        container.Background(Zebra).Border(0.5f).BorderColor(Border).Padding(8).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                for (var i = 0; i < columns; i++) c.RelativeColumn();
            });
            foreach (var (label, value) in items)
            {
                table.Cell().PaddingVertical(3).PaddingRight(8).Column(c =>
                {
                    c.Item().Text(label.ToUpperInvariant()).FontSize(6.5f).SemiBold().FontColor(Subtle).LetterSpacing(0.04f);
                    c.Item().Text(value).FontSize(8.5f);
                });
            }
        });
    }

    public static void Kpi(IContainer container, string label, string value, string? caption = null, string color = Accent)
    {
        container.BorderLeft(3).BorderColor(color).Background(Zebra).PaddingVertical(7).PaddingHorizontal(9).Column(c =>
        {
            c.Item().Text(label.ToUpperInvariant()).FontSize(6.5f).SemiBold().FontColor(Muted).LetterSpacing(0.04f);
            c.Item().PaddingTop(2).Text(value).FontSize(15).Bold();
            if (caption is not null) c.Item().Text(caption).FontSize(7).FontColor(Subtle);
        });
    }

    public static void SectionTitle(IContainer container, string title, string? subtitle = null)
    {
        container.PaddingTop(4).PaddingBottom(4).Column(c =>
        {
            c.Item().Text(title).FontSize(11.5f).Bold().FontColor(AccentDark);
            if (subtitle is not null) c.Item().Text(subtitle).FontSize(7.5f).FontColor(Muted);
        });
    }

    public static void ProgressBar(IContainer container, double percent, string color = Accent)
    {
        var done = (float)Math.Clamp(percent, 0, 100);
        container.Height(6).Row(row =>
        {
            if (done > 0) row.RelativeItem(done).Background(color);
            if (done < 100) row.RelativeItem(100 - done).Background(Border);
        });
    }

    public static IContainer HeaderCell(IContainer container) =>
        container.Background(AccentLight).BorderBottom(1).BorderColor(Accent).PaddingVertical(4).PaddingHorizontal(4);

    public static IContainer BodyCell(IContainer container, int rowIndex) =>
        container.Background(rowIndex % 2 == 1 ? Zebra : "#FFFFFF").BorderBottom(0.5f).BorderColor(Border).PaddingVertical(3.5f).PaddingHorizontal(4);

    public static void HeaderText(IContainer container, string text) =>
        HeaderCell(container).Text(text).FontSize(7).SemiBold().FontColor(AccentDark);

    public static void Pill(IContainer container, string text, string color)
    {
        container.AlignLeft().AlignTop().Background(PillBackgrounds.GetValueOrDefault(color, "#F4F4F4"))
            .PaddingHorizontal(4).PaddingVertical(1)
            .Text(text).FontSize(7).SemiBold().FontColor(color);
    }

    /// <summary>Signature lines for the people who prepare, review and approve the report.</summary>
    public static void SignOff(IContainer container, IReadOnlyList<string> roles)
    {
        container.PaddingTop(16).Row(row =>
        {
            for (var i = 0; i < roles.Count; i++)
            {
                if (i > 0) row.ConstantItem(24);
                var role = roles[i];
                row.RelativeItem().Column(c =>
                {
                    c.Item().Height(28);
                    c.Item().BorderTop(0.75f).BorderColor(Muted).PaddingTop(3).Text(role).FontSize(7.5f).SemiBold();
                    c.Item().Text("Name, signature and date").FontSize(6.5f).FontColor(Subtle);
                });
            }
        });
    }

    public static string Lkr(decimal value) => $"LKR {value:N0}";
}
