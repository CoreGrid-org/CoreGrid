using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Reporting;
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
        // An inverted range would otherwise just return an empty report,
        // indistinguishable from "nothing happened in this period".
        if (filter.From.HasValue && filter.To.HasValue && filter.From.Value > filter.To.Value)
        {
            throw new ValidationException(nameof(filter.To), "The 'to' date must be on or after the 'from' date.");
        }

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

        var discrepanciesTotalCount = await discrepancies.CountAsync(cancellationToken);

        var orderedDiscrepancies = discrepancies.OrderByDescending(d => d.CreatedAt);

        // Page only when the caller asked for a page (the on-screen fetch);
        // the export endpoint leaves Page null and gets every row, same as
        // before this was added.
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? discrepanciesTotalCount;
        pageSize = pageSize < 1 ? 1 : Math.Min(pageSize, 500);

        var pagedDiscrepancies = filter.Page.HasValue
            ? orderedDiscrepancies.Skip((page - 1) * pageSize).Take(pageSize)
            : orderedDiscrepancies;

        var discrepancyRows = await pagedDiscrepancies
            .Select(d => new AuditReportDiscrepancyRow
            {
                AssetCode = d.Asset!.AssetCode,
                AssetName = d.Asset!.Name,
                DepartmentName = d.Asset!.Department!.Name,
                Classification = d.Type.ToString(),
                Status = d.Status.ToString(),
                RaisedAt = d.CreatedAt,
                ResolvedAt = d.ResolvedAt
            })
            .ToListAsync(cancellationToken);

        var totalPages = discrepanciesTotalCount == 0 ? 0 : (int)Math.Ceiling(discrepanciesTotalCount / (double)pageSize);

        return new AuditReportDto
        {
            From = filter.From,
            To = filter.To,
            CampaignsInPeriod = campaignsInPeriod,
            AssetsVerified = assetsVerified,
            AssetsInScope = assetsInScope,
            OpenDiscrepancies = openDiscrepancies,
            ByClassification = byClassification,
            Discrepancies = discrepancyRows,
            DiscrepanciesTotalCount = discrepanciesTotalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    public byte[] BuildCsv(AuditReportDto report)
    {
        var csv = new CsvWriter();

        csv.WriteRow("Audit Campaign Report");
        csv.WriteRow("Period", report.From is null && report.To is null
            ? "All time"
            : $"{report.From?.ToString("yyyy-MM-dd") ?? "…"} to {report.To?.ToString("yyyy-MM-dd") ?? "…"}");
        csv.WriteRow("Generated", report.GeneratedAt.ToString("u"));
        csv.WriteBlankRow();

        csv.WriteRow("Campaigns in period", "Assets in scope", "Assets verified", "Open discrepancies");
        csv.WriteRow(report.CampaignsInPeriod, report.AssetsInScope, report.AssetsVerified, report.OpenDiscrepancies);
        csv.WriteBlankRow();

        csv.WriteRow("Classification", "Raised", "Resolved");
        foreach (var row in report.ByClassification) csv.WriteRow(row.Classification, row.Raised, row.Resolved);
        csv.WriteBlankRow();

        csv.WriteRow("Asset code", "Asset name", "Department", "Classification", "Status", "Raised", "Resolved");
        foreach (var row in report.Discrepancies)
        {
            csv.WriteRow(row.AssetCode, row.AssetName, row.DepartmentName, row.Classification, row.Status,
                row.RaisedAt.ToString("u"), row.ResolvedAt?.ToString("u") ?? "");
        }

        return csv.GetBytes();
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
                        row.RelativeItem().Element(e => PdfComponents.StatBox(e, "Campaigns", report.CampaignsInPeriod.ToString()));
                        row.RelativeItem().Element(e => PdfComponents.StatBox(e, "Assets in scope", report.AssetsInScope.ToString()));
                        row.RelativeItem().Element(e => PdfComponents.StatBox(e, "Verified", report.AssetsVerified.ToString()));
                        row.RelativeItem().Element(e => PdfComponents.StatBox(e, "Open discrepancies", report.OpenDiscrepancies.ToString()));
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

                    column.Item().PaddingTop(8).Text("Discrepancies").FontSize(11).Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Asset").Bold();
                            header.Cell().Text("Department").Bold();
                            header.Cell().Text("Classification").Bold();
                            header.Cell().Text("Raised").Bold();
                            header.Cell().Text("Status").Bold();
                            header.Cell().Text("Resolved").Bold();
                        });

                        if (report.Discrepancies.Count == 0)
                        {
                            table.Cell().ColumnSpan(6).Text("None").FontColor(Colors.Grey.Darken1);
                        }

                        foreach (var row in report.Discrepancies)
                        {
                            table.Cell().Text($"{row.AssetCode} — {row.AssetName}");
                            table.Cell().Text(row.DepartmentName);
                            table.Cell().Text(row.Classification);
                            table.Cell().Text(row.RaisedAt.ToString("yyyy-MM-dd"));
                            table.Cell().Text(row.Status);
                            table.Cell().Text(row.ResolvedAt?.ToString("yyyy-MM-dd") ?? "—");
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
}
