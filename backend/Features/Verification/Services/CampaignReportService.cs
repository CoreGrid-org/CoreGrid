using System.Text;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Verification.DTOs;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CoreGrid.Api.Features.Verification.Services;

// FR-065/FR-084/FR-085: assembles the campaign completion report and
// renders it as CSV or PDF. Kept separate from VerificationCampaignService
// — that one owns campaign CRUD and task generation, this one only reads
// and formats.
public class CampaignReportService : ICampaignReportService
{
    private readonly CoreGridDbContext _db;

    public CampaignReportService(CoreGridDbContext db)
    {
        _db = db;
    }

    public async Task<CampaignReportDto?> GetReportAsync(Guid organizationId, Guid campaignId, CancellationToken cancellationToken)
    {
        var campaign = await _db.VerificationCampaigns
            .AsNoTracking()
            .Include(c => c.ScopeDepartment)
            .Include(c => c.ScopeLocation)
            .Include(c => c.ScopeAssetCategory)
            .Include(c => c.ScopeAssetType)
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.OrganizationId == organizationId, cancellationToken);

        if (campaign is null) return null;

        var tasks = await _db.VerificationTasks
            .AsNoTracking()
            .Include(t => t.Asset)
            .Include(t => t.AssignedToUser)
            .Where(t => t.CampaignId == campaignId)
            .OrderBy(t => t.Asset != null ? t.Asset.AssetCode : string.Empty)
            .ToListAsync(cancellationToken);

        var discrepancies = await _db.Discrepancies
            .AsNoTracking()
            .Include(d => d.Asset)
            .Include(d => d.RaisedByUser)
            .Where(d => d.CampaignId == campaignId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        var scopeParts = new List<string?>
        {
            campaign.ScopeDepartment?.Name,
            campaign.ScopeLocation?.Name,
            campaign.ScopeAssetCategory?.Name,
            campaign.ScopeAssetType?.Name
        }.Where(p => !string.IsNullOrEmpty(p)).ToList();
        var scope = scopeParts.Count > 0 ? string.Join(" · ", scopeParts) : "Whole register";

        var verified = tasks.Count(t => t.Status == VerificationTaskStatus.Completed);

        return new CampaignReportDto
        {
            CampaignId = campaign.Id,
            CampaignName = campaign.Name,
            PeriodStart = campaign.PeriodStart,
            PeriodEnd = campaign.PeriodEnd,
            Scope = scope,
            Status = campaign.Status,
            AssetsInScope = tasks.Count,
            Verified = verified,
            Outstanding = tasks.Count - verified,
            DiscrepanciesByClassification = discrepancies
                .GroupBy(d => d.Type)
                .Select(g => new CampaignReportCount { Label = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(c => c.Count)
                .ToList(),
            DiscrepanciesByResolutionStatus = discrepancies
                .GroupBy(d => d.Status)
                .Select(g => new CampaignReportCount { Label = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(c => c.Count)
                .ToList(),
            Tasks = tasks.Select(t => new CampaignReportTaskRow
            {
                AssetCode = t.Asset?.AssetCode ?? string.Empty,
                AssetName = t.Asset?.Name ?? string.Empty,
                Status = t.Status,
                AssignedToEmail = t.AssignedToUser?.Email,
                DueDate = t.DueDate,
                CompletedAt = t.CompletedAt
            }).ToList(),
            Discrepancies = discrepancies.Select(d => new CampaignReportDiscrepancyRow
            {
                AssetCode = d.Asset?.AssetCode ?? string.Empty,
                Type = d.Type,
                Status = d.Status,
                IsAutomatic = d.IsAutomatic,
                RaisedByEmail = d.RaisedByUser?.Email,
                Description = d.Description,
                ResolutionType = d.ResolutionType,
                ResolvedAt = d.ResolvedAt
            }).ToList(),
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    public byte[] BuildCsv(CampaignReportDto report)
    {
        var sb = new StringBuilder();

        void WriteRow(params object?[] fields) =>
            sb.AppendLine(string.Join(",", fields.Select(CsvEscape)));

        WriteRow("Campaign", report.CampaignName);
        WriteRow("Period", $"{report.PeriodStart:yyyy-MM-dd} to {report.PeriodEnd:yyyy-MM-dd}");
        WriteRow("Scope", report.Scope);
        WriteRow("Status", report.Status);
        WriteRow("Generated", report.GeneratedAt.ToString("u"));
        sb.AppendLine();

        WriteRow("Assets in scope", "Verified", "Outstanding");
        WriteRow(report.AssetsInScope, report.Verified, report.Outstanding);
        sb.AppendLine();

        WriteRow("Discrepancies by classification");
        WriteRow("Classification", "Count");
        foreach (var c in report.DiscrepanciesByClassification) WriteRow(c.Label, c.Count);
        sb.AppendLine();

        WriteRow("Discrepancies by resolution status");
        WriteRow("Status", "Count");
        foreach (var c in report.DiscrepanciesByResolutionStatus) WriteRow(c.Label, c.Count);
        sb.AppendLine();

        WriteRow("Verification tasks");
        WriteRow("Asset Code", "Asset Name", "Status", "Assigned To", "Due Date", "Completed At");
        foreach (var t in report.Tasks)
        {
            WriteRow(
                t.AssetCode,
                t.AssetName,
                t.Status,
                t.AssignedToEmail ?? "Unassigned",
                t.DueDate.ToString("yyyy-MM-dd"),
                t.CompletedAt?.ToString("u") ?? "");
        }
        sb.AppendLine();

        WriteRow("Discrepancies");
        WriteRow("Asset Code", "Classification", "Status", "Raised By", "Description", "Resolution", "Resolved At");
        foreach (var d in report.Discrepancies)
        {
            WriteRow(
                d.AssetCode,
                d.Type,
                d.Status,
                d.IsAutomatic ? "System (auto)" : d.RaisedByEmail ?? "",
                d.Description,
                d.ResolutionType ?? "",
                d.ResolvedAt?.ToString("u") ?? "");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string CsvEscape(object? value)
    {
        var text = value?.ToString() ?? "";
        return text.Contains(',') || text.Contains('"') || text.Contains('\n')
            ? $"\"{text.Replace("\"", "\"\"")}\""
            : text;
    }

    public byte[] BuildPdf(CampaignReportDto report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Item().Text("Verification Campaign Report").FontSize(16).Bold();
                    column.Item().Text(report.CampaignName).FontSize(12).SemiBold();
                    column.Item().PaddingTop(4).Text(
                        $"Period {report.PeriodStart:yyyy-MM-dd} to {report.PeriodEnd:yyyy-MM-dd} · Scope: {report.Scope} · Status: {report.Status}");
                    column.Item().Text($"Generated {report.GeneratedAt:yyyy-MM-dd HH:mm} UTC").FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(12).Column(column =>
                {
                    column.Spacing(14);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Text("Assets in scope").FontColor(Colors.Grey.Darken1);
                            c.Item().Text(report.AssetsInScope.ToString()).FontSize(16).Bold();
                        });
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Text("Verified").FontColor(Colors.Grey.Darken1);
                            c.Item().Text(report.Verified.ToString()).FontSize(16).Bold();
                        });
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Text("Outstanding").FontColor(Colors.Grey.Darken1);
                            c.Item().Text(report.Outstanding.ToString()).FontSize(16).Bold();
                        });
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(e => CountTable(e, "Discrepancies by classification", report.DiscrepanciesByClassification));
                        row.ConstantItem(12);
                        row.RelativeItem().Element(e => CountTable(e, "Discrepancies by resolution status", report.DiscrepanciesByResolutionStatus));
                    });

                    column.Item().Text("Verification tasks").FontSize(11).Bold();
                    column.Item().Element(e => TasksTable(e, report.Tasks));

                    column.Item().Text("Discrepancies").FontSize(11).Bold();
                    column.Item().Element(e => DiscrepanciesTable(e, report.Discrepancies));
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

    private static void CountTable(IContainer container, string title, List<CampaignReportCount> counts)
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

    private static void TasksTable(IContainer container, List<CampaignReportTaskRow> tasks)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);
                c.RelativeColumn(3);
                c.RelativeColumn(2);
                c.RelativeColumn(3);
                c.RelativeColumn(2);
            });

            table.Header(header =>
            {
                header.Cell().Text("Asset").Bold();
                header.Cell().Text("Name").Bold();
                header.Cell().Text("Status").Bold();
                header.Cell().Text("Assigned To").Bold();
                header.Cell().Text("Due").Bold();
            });

            if (tasks.Count == 0)
            {
                table.Cell().ColumnSpan(5).Text("No tasks.").FontColor(Colors.Grey.Darken1);
            }

            foreach (var t in tasks)
            {
                table.Cell().Text(t.AssetCode);
                table.Cell().Text(t.AssetName);
                table.Cell().Text(t.Status.ToString());
                table.Cell().Text(t.AssignedToEmail ?? "Unassigned");
                table.Cell().Text(t.DueDate.ToString("yyyy-MM-dd"));
            }
        });
    }

    private static void DiscrepanciesTable(IContainer container, List<CampaignReportDiscrepancyRow> discrepancies)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(4);
                c.RelativeColumn(2);
            });

            table.Header(header =>
            {
                header.Cell().Text("Asset").Bold();
                header.Cell().Text("Classification").Bold();
                header.Cell().Text("Status").Bold();
                header.Cell().Text("Description").Bold();
                header.Cell().Text("Resolution").Bold();
            });

            if (discrepancies.Count == 0)
            {
                table.Cell().ColumnSpan(5).Text("No discrepancies.").FontColor(Colors.Grey.Darken1);
            }

            foreach (var d in discrepancies)
            {
                table.Cell().Text(d.AssetCode);
                table.Cell().Text(d.Type.ToString());
                table.Cell().Text(d.Status.ToString());
                table.Cell().Text(d.Description);
                table.Cell().Text(d.ResolutionType ?? "—");
            }
        });
    }
}
