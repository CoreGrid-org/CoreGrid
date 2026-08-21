using System.Text;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Verification.DTOs;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CoreGrid.Api.Features.Verification.Services;

// FR-084/FR-085/FR-086: the "Audit Campaign Report" tab on the shared
// Reports page — aggregated across every campaign and discrepancy in scope,
// filterable by date, department, category and discrepancy status. Distinct
// from CampaignReportService, which reports on one specific campaign.
public class AuditReportService : IAuditReportService
{
    private readonly CoreGridDbContext _db;

    public AuditReportService(CoreGridDbContext db)
    {
        _db = db;
    }

    public async Task<AuditReportDto> GetReportAsync(Guid organizationId, AuditReportFilter filter, CancellationToken cancellationToken)
    {
        // Precomputed outside the query — comparing a DateOnly-derived bound
        // against a DateTimeOffset column translates safely; calling
        // DateOnly.FromDateTime(...) *inside* the query does not.
        DateTimeOffset? fromBound = filter.From.HasValue
            ? new DateTimeOffset(filter.From.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : null;
        DateTimeOffset? toBound = filter.To.HasValue
            ? new DateTimeOffset(filter.To.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero)
            : null;

        var campaigns = _db.VerificationCampaigns.AsNoTracking().Where(c => c.OrganizationId == organizationId);
        if (filter.From.HasValue) campaigns = campaigns.Where(c => c.PeriodEnd >= filter.From.Value);
        if (filter.To.HasValue) campaigns = campaigns.Where(c => c.PeriodStart <= filter.To.Value);

        var campaignIds = await campaigns.Select(c => c.Id).ToListAsync(cancellationToken);
        var campaignsInPeriod = campaignIds.Count;

        var tasks = _db.VerificationTasks.AsNoTracking().Where(t => campaignIds.Contains(t.CampaignId));
        if (filter.DepartmentId.HasValue) tasks = tasks.Where(t => t.Asset!.DepartmentId == filter.DepartmentId.Value);
        if (filter.AssetCategoryId.HasValue) tasks = tasks.Where(t => t.Asset!.AssetType!.AssetCategoryId == filter.AssetCategoryId.Value);

        var assetsInScope = await tasks.CountAsync(cancellationToken);
        var assetsVerified = await tasks.CountAsync(t => t.Status == VerificationTaskStatus.Completed, cancellationToken);

        var discrepancies = _db.Discrepancies.AsNoTracking().Where(d => d.OrganizationId == organizationId);
        if (fromBound.HasValue) discrepancies = discrepancies.Where(d => d.CreatedAt >= fromBound.Value);
        if (toBound.HasValue) discrepancies = discrepancies.Where(d => d.CreatedAt <= toBound.Value);
        if (filter.DepartmentId.HasValue) discrepancies = discrepancies.Where(d => d.Asset!.DepartmentId == filter.DepartmentId.Value);
        if (filter.AssetCategoryId.HasValue) discrepancies = discrepancies.Where(d => d.Asset!.AssetType!.AssetCategoryId == filter.AssetCategoryId.Value);
        if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<DiscrepancyStatus>(filter.Status, true, out var statusFilter))
        {
            discrepancies = discrepancies.Where(d => d.Status == statusFilter);
        }

        var openDiscrepancies = await discrepancies.CountAsync(d => d.Status == DiscrepancyStatus.Open, cancellationToken);

        // Materialize before ordering/shaping into the response type — a
        // GroupBy → Select-into-record → further LINQ op doesn't translate
        // (see the fix for the same class of bug in DashboardController).
        var classificationCounts = await discrepancies
            .GroupBy(d => d.Type)
            .Select(g => new
            {
                Type = g.Key,
                Raised = g.Count(),
                Resolved = g.Count(x => x.Status == DiscrepancyStatus.Resolved)
            })
            .ToListAsync(cancellationToken);

        var byClassification = classificationCounts
            .OrderByDescending(c => c.Raised)
            .Select(c => new AuditReportClassificationRow { Classification = c.Type.ToString(), Raised = c.Raised, Resolved = c.Resolved })
            .ToList();

        return new AuditReportDto
        {
            From = filter.From,
            To = filter.To,
            CampaignsInPeriod = campaignsInPeriod,
            AssetsVerified = assetsVerified,
            AssetsInScope = assetsInScope,
            OpenDiscrepancies = openDiscrepancies,
            ByClassification = byClassification,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    public byte[] BuildCsv(AuditReportDto report)
    {
        var sb = new StringBuilder();

        void WriteRow(params object?[] fields) =>
            sb.AppendLine(string.Join(",", fields.Select(CsvEscape)));

        WriteRow("Audit Campaign Report");
        WriteRow("Period", report.From is null && report.To is null
            ? "All time"
            : $"{report.From?.ToString("yyyy-MM-dd") ?? "…"} to {report.To?.ToString("yyyy-MM-dd") ?? "…"}");
        WriteRow("Generated", report.GeneratedAt.ToString("u"));
        sb.AppendLine();

        WriteRow("Campaigns in period", "Assets in scope", "Assets verified", "Open discrepancies");
        WriteRow(report.CampaignsInPeriod, report.AssetsInScope, report.AssetsVerified, report.OpenDiscrepancies);
        sb.AppendLine();

        WriteRow("Classification", "Raised", "Resolved");
        foreach (var row in report.ByClassification) WriteRow(row.Classification, row.Raised, row.Resolved);

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string CsvEscape(object? value)
    {
        var text = value?.ToString() ?? "";
        return text.Contains(',') || text.Contains('"') || text.Contains('\n')
            ? $"\"{text.Replace("\"", "\"\"")}\""
            : text;
    }

    public byte[] BuildPdf(AuditReportDto report)
    {
        var periodLabel = report.From is null && report.To is null
            ? "All time"
            : $"{report.From?.ToString("yyyy-MM-dd") ?? "…"} to {report.To?.ToString("yyyy-MM-dd") ?? "…"}";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Item().Text("Audit Campaign Report").FontSize(16).Bold();
                    column.Item().PaddingTop(4).Text($"Period: {periodLabel}");
                    column.Item().Text($"Generated {report.GeneratedAt:yyyy-MM-dd HH:mm} UTC").FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(12).Column(column =>
                {
                    column.Spacing(14);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(e => StatBox(e, "Campaigns", report.CampaignsInPeriod.ToString()));
                        row.RelativeItem().Element(e => StatBox(e, "Assets in scope", report.AssetsInScope.ToString()));
                        row.RelativeItem().Element(e => StatBox(e, "Verified", report.AssetsVerified.ToString()));
                        row.RelativeItem().Element(e => StatBox(e, "Open discrepancies", report.OpenDiscrepancies.ToString()));
                    });

                    column.Item().Text("Discrepancies by classification").FontSize(11).Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Classification").Bold();
                            header.Cell().Text("Raised").Bold();
                            header.Cell().Text("Resolved").Bold();
                        });

                        if (report.ByClassification.Count == 0)
                        {
                            table.Cell().ColumnSpan(3).Text("None").FontColor(Colors.Grey.Darken1);
                        }

                        foreach (var row in report.ByClassification)
                        {
                            table.Cell().Text(row.Classification);
                            table.Cell().Text(row.Raised.ToString());
                            table.Cell().Text(row.Resolved.ToString());
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void StatBox(IContainer container, string label, string value)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
        {
            c.Item().Text(label).FontColor(Colors.Grey.Darken1);
            c.Item().Text(value).FontSize(16).Bold();
        });
    }
}
