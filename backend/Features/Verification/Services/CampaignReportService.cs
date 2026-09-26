using System.Text.RegularExpressions;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.Reporting;
using CoreGrid.Api.Features.Shared.Storage;
using CoreGrid.Api.Features.Verification.DTOs;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CoreGrid.Api.Features.Verification.Services;

// FR-065/FR-084/FR-085: assembles the campaign completion report and renders
// it as CSV or PDF. Kept separate from VerificationCampaignService, which owns
// campaign CRUD and task generation; this one only reads and formats.
public class CampaignReportService(CoreGridDbContext db, IFileStorageService? storage = null) : ICampaignReportService
{
    public async Task<CampaignReportDto?> GetReportAsync(Guid organizationId, Guid campaignId, CancellationToken cancellationToken)
    {
        var campaign = await db.VerificationCampaigns
            .AsNoTracking()
            .Include(c => c.Organization)
            .Include(c => c.CreatedByUser)
            .Include(c => c.ScopeDepartment)
            .Include(c => c.ScopeLocation)
            .Include(c => c.ScopeAssetCategory)
            .Include(c => c.ScopeAssetType)
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.OrganizationId == organizationId, cancellationToken);

        if (campaign is null) return null;

        var tasks = await db.VerificationTasks
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Asset).ThenInclude(a => a!.AssetType)
            .Include(t => t.Asset).ThenInclude(a => a!.Department)
            .Include(t => t.Asset).ThenInclude(a => a!.Location)
            .Include(t => t.AssignedToUser)
            .Include(t => t.CompletedByUser)
            .Include(t => t.AssertedLocation)
            .Where(t => t.CampaignId == campaignId)
            .ToListAsync(cancellationToken);
        tasks = tasks.OrderBy(t => t.Asset?.Department?.Name).ThenBy(t => t.Asset?.AssetCode).ToList();

        var discrepancies = await db.Discrepancies
            .AsNoTracking()
            .AsSplitQuery()
            .Include(d => d.Asset).ThenInclude(a => a!.Department)
            .Include(d => d.RaisedByUser)
            .Include(d => d.ResolvedByUser)
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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var taskRows = tasks.Select(t => ToTaskRow(t, campaign.Status, today)).ToList();
        var discrepancyRows = new List<CampaignReportDiscrepancyRow>();
        foreach (var d in discrepancies) discrepancyRows.Add(await ToDiscrepancyRowAsync(d, cancellationToken));

        var verified = taskRows.Count(t => t.Status == VerificationTaskStatus.Completed);
        var discrepanciesByAsset = discrepancies.GroupBy(d => d.AssetId).ToDictionary(g => g.Key, g => g.Count());

        return new CampaignReportDto
        {
            CampaignId = campaign.Id,
            CampaignName = campaign.Name,
            PeriodStart = campaign.PeriodStart,
            PeriodEnd = campaign.PeriodEnd,
            Scope = scope,
            Status = campaign.Status,
            OrganizationName = campaign.Organization?.Name,
            CreatedByName = DisplayName(campaign.CreatedByUser),
            CreatedAt = campaign.CreatedAt,

            AssetsInScope = taskRows.Count,
            Verified = verified,
            Outstanding = taskRows.Count - verified,
            OverdueTasks = taskRows.Count(t => t.IsOverdue),
            CompletionPercent = Percent(verified, taskRows.Count),

            FoundAsRecorded = taskRows.Count(t => t.Outcome == VerificationOutcomes.Verified),
            NotFound = taskRows.Count(t => t.Outcome == VerificationOutcomes.NotFound),
            LocationMismatches = taskRows.Count(t => t.Outcome is VerificationOutcomes.LocationMismatch or VerificationOutcomes.LocationAndConditionMismatch),
            ConditionMismatches = taskRows.Count(t => t.Outcome is VerificationOutcomes.ConditionMismatch or VerificationOutcomes.LocationAndConditionMismatch),

            OpenDiscrepancies = discrepancies.Count(d => d.Status == DiscrepancyStatus.Open),
            ResolvedDiscrepancies = discrepancies.Count(d => d.Status == DiscrepancyStatus.Resolved),
            ValueInScope = taskRows.Sum(t => t.AcquisitionCost),
            ValueNotFound = taskRows.Where(t => t.Outcome == VerificationOutcomes.NotFound).Sum(t => t.AcquisitionCost),

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
            ByDepartment = tasks
                .GroupBy(t => t.Asset?.Department?.Name ?? "Unassigned")
                .Select(g =>
                {
                    var done = g.Count(t => t.Status == VerificationTaskStatus.Completed);
                    return new CampaignReportDepartmentRow
                    {
                        Department = g.Key,
                        AssetsInScope = g.Count(),
                        Verified = done,
                        Outstanding = g.Count() - done,
                        Discrepancies = g.Sum(t => discrepanciesByAsset.GetValueOrDefault(t.AssetId)),
                        CompletionPercent = Percent(done, g.Count()),
                        ValueInScope = g.Sum(t => t.Asset?.AcquisitionCost ?? 0),
                    };
                })
                .OrderBy(r => r.Department)
                .ToList(),
            ByVerifier = taskRows
                .GroupBy(t => t.AssignedToName ?? "Unassigned")
                .Select(g => new CampaignReportVerifierRow
                {
                    Name = g.Key,
                    Email = g.First().AssignedToEmail,
                    Assigned = g.Count(),
                    Completed = g.Count(t => t.Status == VerificationTaskStatus.Completed),
                    IssuesFound = g.Count(t => t.Outcome is not (VerificationOutcomes.Verified or VerificationOutcomes.Pending)),
                })
                .OrderBy(r => r.Name == "Unassigned").ThenBy(r => r.Name)
                .ToList(),
            Tasks = taskRows,
            Discrepancies = discrepancyRows,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    private static CampaignReportTaskRow ToTaskRow(VerificationTask t, CampaignStatus campaignStatus, DateOnly today)
    {
        var asset = t.Asset;
        var completed = t.Status == VerificationTaskStatus.Completed;
        var locationDiffers = completed && t.AssertedPresent == true && t.AssertedLocationId.HasValue && t.AssertedLocationId != asset?.LocationId;
        var conditionDiffers = completed && t.AssertedPresent == true && !string.IsNullOrWhiteSpace(t.AssertedCondition)
            && !string.Equals(t.AssertedCondition, asset?.Condition, StringComparison.OrdinalIgnoreCase);

        var outcome = !completed ? VerificationOutcomes.Pending
            : t.AssertedPresent == false ? VerificationOutcomes.NotFound
            : locationDiffers && conditionDiffers ? VerificationOutcomes.LocationAndConditionMismatch
            : locationDiffers ? VerificationOutcomes.LocationMismatch
            : conditionDiffers ? VerificationOutcomes.ConditionMismatch
            : VerificationOutcomes.Verified;

        return new CampaignReportTaskRow
        {
            AssetCode = asset?.AssetCode ?? string.Empty,
            AssetName = asset?.Name ?? string.Empty,
            AssetType = asset?.AssetType?.Name,
            Department = asset?.Department?.Name,
            AcquisitionCost = asset?.AcquisitionCost ?? 0,
            Status = t.Status,
            Outcome = outcome,
            IsOverdue = !completed && campaignStatus == CampaignStatus.Active && t.DueDate < today,
            AssignedToEmail = t.AssignedToUser?.Email,
            AssignedToName = DisplayName(t.AssignedToUser),
            DueDate = t.DueDate,
            CompletedAt = t.CompletedAt,
            CompletedByName = DisplayName(t.CompletedByUser),
            RecordedLocation = asset?.Location?.Name,
            RecordedCondition = asset?.Condition,
            AssertedPresent = completed ? t.AssertedPresent : null,
            AssertedLocation = t.AssertedLocation?.Name,
            AssertedCondition = t.AssertedCondition,
        };
    }

    private async Task<CampaignReportDiscrepancyRow> ToDiscrepancyRowAsync(Discrepancy d, CancellationToken cancellationToken) => new()
    {
        Id = d.Id,
        AssetCode = d.Asset?.AssetCode ?? string.Empty,
        AssetName = d.Asset?.Name,
        Department = d.Asset?.Department?.Name,
        Type = d.Type,
        Status = d.Status,
        IsAutomatic = d.IsAutomatic,
        RaisedByEmail = d.RaisedByUser?.Email,
        RaisedByName = DisplayName(d.RaisedByUser),
        RaisedAt = d.CreatedAt,
        Description = d.Description,
        ResolutionType = d.ResolutionType,
        ResolutionExplanation = d.ResolutionExplanation,
        CorrectiveAction = d.CorrectiveAction,
        RegisterCorrected = d.RegisterCorrected,
        ResolvedByName = DisplayName(d.ResolvedByUser),
        ResolvedAt = d.ResolvedAt,
        HasPhoto = !string.IsNullOrWhiteSpace(d.PhotoUrl),
        PhotoUrl = storage is null ? null : await PhotoKeys.ToDisplayUrlAsync(storage, d.PhotoUrl, cancellationToken),
    };

    // ─── Labels ──────────────────────────────────────────────────────────────

    private static string? DisplayName(User? user) =>
        user is null ? null : $"{user.GivenName} {user.FamilyName}".Trim() is { Length: > 0 } name ? name : user.Email;

    private static double Percent(int part, int whole) => whole == 0 ? 0 : Math.Round(100.0 * part / whole, 1);

    // "LocationMismatch" / "REGISTER_CORRECTED" -> "Location mismatch" / "Register corrected".
    public static string Humanize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "-";
        var spaced = value.Contains('_') ? value.Replace('_', ' ').ToLowerInvariant() : Regex.Replace(value, "(?<=[a-z])(?=[A-Z])", " ").ToLowerInvariant();
        return char.ToUpperInvariant(spaced[0]) + spaced[1..];
    }

    private static string OutcomeLabel(string outcome) => outcome switch
    {
        VerificationOutcomes.Verified => "Verified",
        VerificationOutcomes.NotFound => "Not found",
        VerificationOutcomes.LocationMismatch => "Location mismatch",
        VerificationOutcomes.ConditionMismatch => "Condition mismatch",
        VerificationOutcomes.LocationAndConditionMismatch => "Location & condition",
        _ => "Pending",
    };

    private static string OutcomeColor(string outcome) => outcome switch
    {
        VerificationOutcomes.Verified => ReportPdf.Green,
        VerificationOutcomes.NotFound => ReportPdf.Red,
        VerificationOutcomes.Pending => ReportPdf.Grey,
        _ => ReportPdf.Amber,
    };

    private static string Date(DateTimeOffset? value) => value?.ToString("yyyy-MM-dd") ?? "-";

    // ─── CSV ─────────────────────────────────────────────────────────────────

    public byte[] BuildCsv(CampaignReportDto report)
    {
        var csv = new CsvWriter();

        csv.WriteRow("Verification Campaign Report");
        csv.WriteRow("Organisation", report.OrganizationName ?? "");
        csv.WriteRow("Campaign", report.CampaignName);
        csv.WriteRow("Period", $"{report.PeriodStart:yyyy-MM-dd} to {report.PeriodEnd:yyyy-MM-dd}");
        csv.WriteRow("Scope", report.Scope);
        csv.WriteRow("Status", report.Status);
        csv.WriteRow("Created by", report.CreatedByName ?? "", Date(report.CreatedAt));
        csv.WriteRow("Generated", report.GeneratedAt.ToString("yyyy-MM-dd HH:mm 'UTC'"), report.GeneratedByName ?? "");
        csv.WriteBlankRow();

        csv.WriteRow("Summary");
        csv.WriteRow("Assets in scope", "Verified", "Outstanding", "Overdue", "Completion %", "Found as recorded", "Not found",
            "Location mismatches", "Condition mismatches", "Open discrepancies", "Resolved discrepancies", "Value in scope (LKR)", "Value not found (LKR)");
        csv.WriteRow(report.AssetsInScope, report.Verified, report.Outstanding, report.OverdueTasks, report.CompletionPercent,
            report.FoundAsRecorded, report.NotFound, report.LocationMismatches, report.ConditionMismatches,
            report.OpenDiscrepancies, report.ResolvedDiscrepancies, report.ValueInScope, report.ValueNotFound);
        csv.WriteBlankRow();

        csv.WriteRow("By department");
        csv.WriteRow("Department", "Assets in scope", "Verified", "Outstanding", "Discrepancies", "Completion %", "Value in scope (LKR)");
        foreach (var r in report.ByDepartment)
            csv.WriteRow(r.Department, r.AssetsInScope, r.Verified, r.Outstanding, r.Discrepancies, r.CompletionPercent, r.ValueInScope);
        csv.WriteBlankRow();

        csv.WriteRow("By verifier");
        csv.WriteRow("Verifier", "Email", "Assigned", "Completed", "Issues found");
        foreach (var r in report.ByVerifier) csv.WriteRow(r.Name, r.Email ?? "", r.Assigned, r.Completed, r.IssuesFound);
        csv.WriteBlankRow();

        csv.WriteRow("Asset verification register");
        csv.WriteRow("Asset code", "Asset name", "Type", "Department", "Acquisition cost (LKR)", "Outcome", "Task status", "Overdue",
            "Recorded location", "Found at", "Recorded condition", "Observed condition", "Present",
            "Assigned to", "Due date", "Verified by", "Verified at");
        foreach (var t in report.Tasks)
        {
            csv.WriteRow(t.AssetCode, t.AssetName, t.AssetType ?? "", t.Department ?? "", t.AcquisitionCost, OutcomeLabel(t.Outcome),
                t.Status, t.IsOverdue ? "Yes" : "No",
                t.RecordedLocation ?? "", t.AssertedLocation ?? "", Humanize(t.RecordedCondition), t.AssertedCondition is null ? "" : Humanize(t.AssertedCondition),
                t.AssertedPresent is null ? "" : t.AssertedPresent.Value ? "Yes" : "No",
                t.AssignedToName ?? "Unassigned", t.DueDate.ToString("yyyy-MM-dd"), t.CompletedByName ?? "", Date(t.CompletedAt));
        }
        csv.WriteBlankRow();

        csv.WriteRow("Discrepancy register");
        csv.WriteRow("Asset code", "Asset name", "Department", "Classification", "Status", "Raised by", "Raised at", "Description",
            "Resolution", "Explanation", "Corrective action", "Register corrected", "Resolved by", "Resolved at", "Photo");
        foreach (var d in report.Discrepancies)
        {
            csv.WriteRow(d.AssetCode, d.AssetName ?? "", d.Department ?? "", Humanize(d.Type.ToString()), d.Status,
                d.IsAutomatic ? $"{d.RaisedByName ?? "System"} (automatic)" : d.RaisedByName ?? "", Date(d.RaisedAt), d.Description,
                d.ResolutionType is null ? "" : Humanize(d.ResolutionType), d.ResolutionExplanation ?? "", d.CorrectiveAction ?? "",
                d.RegisterCorrected ? "Yes" : "No", d.ResolvedByName ?? "", Date(d.ResolvedAt), d.HasPhoto ? "Yes" : "No");
        }

        return csv.GetBytes();
    }

    // ─── PDF ─────────────────────────────────────────────────────────────────

    public byte[] BuildPdf(CampaignReportDto report)
    {
        var organization = report.OrganizationName ?? "CoreGrid";
        var subtitle = $"{report.CampaignName}  ·  {report.PeriodStart:yyyy-MM-dd} to {report.PeriodEnd:yyyy-MM-dd}";
        var footer = $"{organization} · Verification campaign report · Generated {report.GeneratedAt:yyyy-MM-dd HH:mm} UTC"
            + (report.GeneratedByName is null ? "" : $" by {report.GeneratedByName}") + " · Confidential";

        var document = Document.Create(container =>
        {
            // Page 1+: executive summary (portrait).
            container.Page(page =>
            {
                ReportPdf.ConfigurePage(page, PageSizes.A4);
                page.Header().Element(e => ReportPdf.Header(e, organization, "Verification Campaign Report", subtitle));
                page.Footer().Element(e => ReportPdf.Footer(e, footer));
                page.Content().Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Element(e => ReportPdf.MetaGrid(e,
                    [
                        ("Campaign", report.CampaignName),
                        ("Status", report.Status.ToString()),
                        ("Scope", report.Scope),
                        ("Period", $"{report.PeriodStart:yyyy-MM-dd} to {report.PeriodEnd:yyyy-MM-dd}"),
                        ("Created by", $"{report.CreatedByName ?? "-"} on {Date(report.CreatedAt)}"),
                        ("Generated", $"{report.GeneratedAt:yyyy-MM-dd HH:mm} UTC" + (report.GeneratedByName is null ? "" : $" by {report.GeneratedByName}")),
                    ]));

                    column.Item().Element(e => ReportPdf.SectionTitle(e, "Summary"));
                    column.Item().Row(row =>
                    {
                        row.Spacing(8);
                        row.RelativeItem().Element(e => ReportPdf.Kpi(e, "Assets in scope", report.AssetsInScope.ToString(), ReportPdf.Lkr(report.ValueInScope)));
                        row.RelativeItem().Element(e => ReportPdf.Kpi(e, "Verified", report.Verified.ToString(), $"{report.CompletionPercent:0.#}% complete", ReportPdf.Green));
                        row.RelativeItem().Element(e => ReportPdf.Kpi(e, "Outstanding", report.Outstanding.ToString(),
                            report.OverdueTasks > 0 ? $"{report.OverdueTasks} overdue" : "none overdue", report.OverdueTasks > 0 ? ReportPdf.Amber : ReportPdf.Grey));
                        row.RelativeItem().Element(e => ReportPdf.Kpi(e, "Open discrepancies", report.OpenDiscrepancies.ToString(),
                            $"{report.ResolvedDiscrepancies} resolved", report.OpenDiscrepancies > 0 ? ReportPdf.Red : ReportPdf.Green));
                    });
                    column.Item().Column(c =>
                    {
                        c.Item().PaddingBottom(3).Text($"Verification progress: {report.Verified} of {report.AssetsInScope} assets ({report.CompletionPercent:0.#}%)").FontSize(7.5f).FontColor(ReportPdf.Muted);
                        c.Item().Element(e => ReportPdf.ProgressBar(e, report.CompletionPercent));
                    });

                    column.Item().Element(e => ReportPdf.SectionTitle(e, "Key findings", "Completed verifications compared with the asset register."));
                    column.Item().Row(row =>
                    {
                        row.Spacing(8);
                        row.RelativeItem().Element(e => ReportPdf.Kpi(e, "Found as recorded", report.FoundAsRecorded.ToString(), null, ReportPdf.Green));
                        row.RelativeItem().Element(e => ReportPdf.Kpi(e, "Not found", report.NotFound.ToString(),
                            report.NotFound > 0 ? $"{ReportPdf.Lkr(report.ValueNotFound)} at risk" : null, report.NotFound > 0 ? ReportPdf.Red : ReportPdf.Grey));
                        row.RelativeItem().Element(e => ReportPdf.Kpi(e, "Location mismatches", report.LocationMismatches.ToString(), null, ReportPdf.Amber));
                        row.RelativeItem().Element(e => ReportPdf.Kpi(e, "Condition mismatches", report.ConditionMismatches.ToString(), null, ReportPdf.Amber));
                    });

                    column.Item().Element(e => ReportPdf.SectionTitle(e, "By department"));
                    column.Item().Element(e => DepartmentTable(e, report.ByDepartment));

                    column.Item().Row(row =>
                    {
                        row.Spacing(12);
                        row.RelativeItem(3).Column(c =>
                        {
                            c.Item().Element(e => ReportPdf.SectionTitle(e, "By verifier"));
                            c.Item().Element(e => VerifierTable(e, report.ByVerifier));
                        });
                        row.RelativeItem(2).Column(c =>
                        {
                            c.Item().Element(e => ReportPdf.SectionTitle(e, "Discrepancies by type"));
                            c.Item().Element(e => CountTable(e, report.DiscrepanciesByClassification));
                        });
                    });
                });
            });

            // Registers (landscape, so every column fits).
            container.Page(page =>
            {
                ReportPdf.ConfigurePage(page, PageSizes.A4.Landscape());
                page.Header().Element(e => ReportPdf.Header(e, organization, "Asset Verification Register", subtitle));
                page.Footer().Element(e => ReportPdf.Footer(e, footer));
                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text($"{report.Tasks.Count} assets in scope. Recorded values are the register as at {report.GeneratedAt:yyyy-MM-dd}.").FontSize(7.5f).FontColor(ReportPdf.Muted);
                    column.Item().Element(e => TasksTable(e, report.Tasks));
                });
            });

            container.Page(page =>
            {
                ReportPdf.ConfigurePage(page, PageSizes.A4.Landscape());
                page.Header().Element(e => ReportPdf.Header(e, organization, "Discrepancy Register", subtitle));
                page.Footer().Element(e => ReportPdf.Footer(e, footer));
                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text($"{report.Discrepancies.Count} discrepancies raised during this campaign: {report.OpenDiscrepancies} open, {report.ResolvedDiscrepancies} resolved.").FontSize(7.5f).FontColor(ReportPdf.Muted);
                    column.Item().Element(e => DiscrepanciesTable(e, report.Discrepancies));

                    column.Item().PaddingTop(8).Element(e => ReportPdf.SectionTitle(e, "Sign-off", "Confirms the findings above have been reviewed."));
                    column.Item().Element(e => ReportPdf.SignOff(e, ["Prepared by (Auditor)", "Reviewed by", "Approved by"]));
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void DepartmentTable(IContainer container, List<CampaignReportDepartmentRow> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(4);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(4);
                c.RelativeColumn(3);
            });
            table.Header(h =>
            {
                foreach (var title in new[] { "Department", "In scope", "Verified", "Outstanding", "Discrepancies", "Completion", "Value (LKR)" })
                    h.Cell().Element(e => ReportPdf.HeaderText(e, title));
            });
            if (rows.Count == 0) table.Cell().ColumnSpan(7).Element(e => ReportPdf.BodyCell(e, 0)).Text("No assets in scope.").FontColor(ReportPdf.Subtle);
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var index = i;
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(r.Department).SemiBold();
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(r.AssetsInScope.ToString());
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(r.Verified.ToString());
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(r.Outstanding.ToString());
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(r.Discrepancies.ToString()).FontColor(r.Discrepancies > 0 ? ReportPdf.Red : ReportPdf.Text);
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Row(row =>
                {
                    row.ConstantItem(32).Text($"{r.CompletionPercent:0}%");
                    row.RelativeItem().PaddingTop(3).Element(e => ReportPdf.ProgressBar(e, r.CompletionPercent));
                });
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).AlignRight().Text($"{r.ValueInScope:N0}");
            }
        });
    }

    private static void VerifierTable(IContainer container, List<CampaignReportVerifierRow> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(4);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
            });
            table.Header(h =>
            {
                foreach (var title in new[] { "Verifier", "Assigned", "Completed", "Issues" })
                    h.Cell().Element(e => ReportPdf.HeaderText(e, title));
            });
            if (rows.Count == 0) table.Cell().ColumnSpan(4).Element(e => ReportPdf.BodyCell(e, 0)).Text("No tasks.").FontColor(ReportPdf.Subtle);
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var index = i;
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(r.Name);
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(r.Assigned.ToString());
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(r.Completed.ToString());
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(r.IssuesFound.ToString()).FontColor(r.IssuesFound > 0 ? ReportPdf.Red : ReportPdf.Text);
            }
        });
    }

    private static void CountTable(IContainer container, List<CampaignReportCount> counts)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(4);
                c.RelativeColumn(1);
            });
            table.Header(h =>
            {
                h.Cell().Element(e => ReportPdf.HeaderText(e, "Type"));
                h.Cell().Element(e => ReportPdf.HeaderText(e, "Count"));
            });
            if (counts.Count == 0) table.Cell().ColumnSpan(2).Element(e => ReportPdf.BodyCell(e, 0)).Text("None raised.").FontColor(ReportPdf.Subtle);
            for (var i = 0; i < counts.Count; i++)
            {
                var c = counts[i];
                var index = i;
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(Humanize(c.Label));
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(c.Count.ToString());
            }
        });
    }

    private static void TasksTable(IContainer container, List<CampaignReportTaskRow> tasks)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(3.3f); // code
                c.RelativeColumn(3.5f); // name
                c.RelativeColumn(2.6f); // department
                c.RelativeColumn(2.8f); // outcome
                c.RelativeColumn(3.2f); // recorded location
                c.RelativeColumn(3.2f); // found at
                c.RelativeColumn(2.2f); // condition recorded -> observed
                c.RelativeColumn(2.8f); // verifier
                c.RelativeColumn(1.8f); // date
            });
            table.Header(h =>
            {
                foreach (var title in new[] { "Asset code", "Asset", "Department", "Outcome", "Recorded location", "Found at", "Condition", "Verified by", "Date" })
                    h.Cell().Element(e => ReportPdf.HeaderText(e, title));
            });
            if (tasks.Count == 0) table.Cell().ColumnSpan(9).Element(e => ReportPdf.BodyCell(e, 0)).Text("No assets in scope.").FontColor(ReportPdf.Subtle);
            for (var i = 0; i < tasks.Count; i++)
            {
                var t = tasks[i];
                var index = i;
                var locationFlag = t.Outcome is VerificationOutcomes.LocationMismatch or VerificationOutcomes.LocationAndConditionMismatch;
                var conditionFlag = t.Outcome is VerificationOutcomes.ConditionMismatch or VerificationOutcomes.LocationAndConditionMismatch;

                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(t.AssetCode).FontFamily(Fonts.CourierNew).FontSize(7.5f);
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Column(c =>
                {
                    c.Item().Text(t.AssetName);
                    if (t.AssetType is not null) c.Item().Text(t.AssetType).FontSize(7).FontColor(ReportPdf.Subtle);
                });
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(t.Department ?? "-");
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Column(c =>
                {
                    c.Item().Element(p => ReportPdf.Pill(p, OutcomeLabel(t.Outcome), OutcomeColor(t.Outcome)));
                    if (t.IsOverdue) c.Item().PaddingTop(1).Text("Overdue").FontSize(6.5f).SemiBold().FontColor(ReportPdf.Red);
                });
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(t.RecordedLocation ?? "-");
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(
                        t.AssertedPresent == false ? "Not found" : t.AssertedLocation ?? (t.Outcome == VerificationOutcomes.Pending ? "-" : t.RecordedLocation ?? "-"))
                    .FontColor(locationFlag || t.AssertedPresent == false ? ReportPdf.Red : ReportPdf.Text);
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(
                        conditionFlag ? $"{Humanize(t.RecordedCondition)} → {Humanize(t.AssertedCondition)}" : Humanize(t.RecordedCondition))
                    .FontColor(conditionFlag ? ReportPdf.Amber : ReportPdf.Text);
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(t.CompletedByName ?? (t.AssignedToName is null ? "Unassigned" : $"{t.AssignedToName} (assigned)"));
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(t.CompletedAt is null ? $"due {t.DueDate:yyyy-MM-dd}" : Date(t.CompletedAt));
            }
        });
    }

    private static void DiscrepanciesTable(IContainer container, List<CampaignReportDiscrepancyRow> discrepancies)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(3.3f); // asset
                c.RelativeColumn(2.4f); // type
                c.RelativeColumn(1.6f); // status
                c.RelativeColumn(5.5f); // description
                c.RelativeColumn(2.6f); // raised
                c.RelativeColumn(6f);   // resolution
            });
            table.Header(h =>
            {
                foreach (var title in new[] { "Asset", "Classification", "Status", "Finding", "Raised", "Resolution" })
                    h.Cell().Element(e => ReportPdf.HeaderText(e, title));
            });
            if (discrepancies.Count == 0) table.Cell().ColumnSpan(6).Element(e => ReportPdf.BodyCell(e, 0)).Text("No discrepancies were raised.").FontColor(ReportPdf.Subtle);
            for (var i = 0; i < discrepancies.Count; i++)
            {
                var d = discrepancies[i];
                var index = i;
                var open = d.Status == DiscrepancyStatus.Open;

                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Column(c =>
                {
                    c.Item().Text(d.AssetCode).FontFamily(Fonts.CourierNew).FontSize(7.5f);
                    if (d.AssetName is not null) c.Item().Text(d.AssetName).FontSize(7).FontColor(ReportPdf.Subtle);
                });
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Text(Humanize(d.Type.ToString()));
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Element(p => ReportPdf.Pill(p, open ? "Open" : "Resolved", open ? ReportPdf.Red : ReportPdf.Green));
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Column(c =>
                {
                    c.Item().Text(d.Description);
                    if (d.HasPhoto) c.Item().PaddingTop(1).Text("Photo evidence on file").FontSize(6.5f).FontColor(ReportPdf.Accent);
                });
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Column(c =>
                {
                    c.Item().Text(Date(d.RaisedAt));
                    c.Item().Text(d.IsAutomatic ? $"{d.RaisedByName ?? "System"} (auto)" : d.RaisedByName ?? "-").FontSize(7).FontColor(ReportPdf.Subtle);
                });
                table.Cell().Element(e => ReportPdf.BodyCell(e, index)).Column(c =>
                {
                    if (open)
                    {
                        c.Item().Text("Awaiting resolution").FontColor(ReportPdf.Subtle);
                        return;
                    }
                    c.Item().Text(Humanize(d.ResolutionType)).SemiBold();
                    if (d.ResolutionExplanation is not null) c.Item().Text(d.ResolutionExplanation);
                    if (d.CorrectiveAction is not null) c.Item().Text($"Action: {d.CorrectiveAction}").FontSize(7).FontColor(ReportPdf.Muted);
                    c.Item().Text($"{d.ResolvedByName ?? "-"} · {Date(d.ResolvedAt)}{(d.RegisterCorrected ? " · register corrected" : "")}").FontSize(7).FontColor(ReportPdf.Subtle);
                });
            }
        });
    }
}
