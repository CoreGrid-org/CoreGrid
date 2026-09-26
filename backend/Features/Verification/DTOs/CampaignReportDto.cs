using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Verification.DTOs;

// The campaign completion report (FR-065): what was in scope, what the
// verifiers found against the register, and every discrepancy with its
// resolution. Shared by the on-screen report and the PDF/CSV exports.
public class CampaignReportDto
{
    public Guid CampaignId { get; set; }
    public required string CampaignName { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public required string Scope { get; set; }
    public CampaignStatus Status { get; set; }

    public string? OrganizationName { get; set; }
    public string? CreatedByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Who generated this copy of the report; set by the controller.</summary>
    public string? GeneratedByName { get; set; }

    public int AssetsInScope { get; set; }
    public int Verified { get; set; }
    public int Outstanding { get; set; }
    public int OverdueTasks { get; set; }
    public double CompletionPercent { get; set; }

    // Findings from completed tasks, compared with the register.
    public int FoundAsRecorded { get; set; }
    public int NotFound { get; set; }
    public int LocationMismatches { get; set; }
    public int ConditionMismatches { get; set; }

    public int OpenDiscrepancies { get; set; }
    public int ResolvedDiscrepancies { get; set; }

    // Acquisition value (LKR) of the assets in scope, and of those reported not found.
    public decimal ValueInScope { get; set; }
    public decimal ValueNotFound { get; set; }

    public List<CampaignReportCount> DiscrepanciesByClassification { get; set; } = [];
    public List<CampaignReportCount> DiscrepanciesByResolutionStatus { get; set; } = [];
    public List<CampaignReportDepartmentRow> ByDepartment { get; set; } = [];
    public List<CampaignReportVerifierRow> ByVerifier { get; set; } = [];

    public List<CampaignReportTaskRow> Tasks { get; set; } = [];
    public List<CampaignReportDiscrepancyRow> Discrepancies { get; set; } = [];

    public DateTimeOffset GeneratedAt { get; set; }
}

public class CampaignReportCount
{
    public required string Label { get; set; }
    public int Count { get; set; }
}

public class CampaignReportDepartmentRow
{
    public required string Department { get; set; }
    public int AssetsInScope { get; set; }
    public int Verified { get; set; }
    public int Outstanding { get; set; }
    public int Discrepancies { get; set; }
    public double CompletionPercent { get; set; }
    public decimal ValueInScope { get; set; }
}

public class CampaignReportVerifierRow
{
    public required string Name { get; set; }
    public string? Email { get; set; }
    public int Assigned { get; set; }
    public int Completed { get; set; }
    public int IssuesFound { get; set; }
}

// Outcome of one asset's verification against the register.
public static class VerificationOutcomes
{
    public const string Pending = "Pending";
    public const string Verified = "Verified";
    public const string NotFound = "NotFound";
    public const string LocationMismatch = "LocationMismatch";
    public const string ConditionMismatch = "ConditionMismatch";
    public const string LocationAndConditionMismatch = "LocationAndConditionMismatch";
}

public class CampaignReportTaskRow
{
    public required string AssetCode { get; set; }
    public required string AssetName { get; set; }
    public string? AssetType { get; set; }
    public string? Department { get; set; }
    public decimal AcquisitionCost { get; set; }

    public VerificationTaskStatus Status { get; set; }
    /// <summary>One of <see cref="VerificationOutcomes"/>.</summary>
    public required string Outcome { get; set; }
    public bool IsOverdue { get; set; }

    public string? AssignedToEmail { get; set; }
    public string? AssignedToName { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CompletedByName { get; set; }

    // Register (as at report time) vs what the verifier asserted.
    public string? RecordedLocation { get; set; }
    public string? RecordedCondition { get; set; }
    public bool? AssertedPresent { get; set; }
    public string? AssertedLocation { get; set; }
    public string? AssertedCondition { get; set; }
}

public class CampaignReportDiscrepancyRow
{
    public Guid Id { get; set; }
    public required string AssetCode { get; set; }
    public string? AssetName { get; set; }
    public string? Department { get; set; }
    public DiscrepancyType Type { get; set; }
    public DiscrepancyStatus Status { get; set; }
    public bool IsAutomatic { get; set; }
    public string? RaisedByEmail { get; set; }
    public string? RaisedByName { get; set; }
    public DateTimeOffset RaisedAt { get; set; }
    public required string Description { get; set; }
    public string? ResolutionType { get; set; }
    public string? ResolutionExplanation { get; set; }
    public string? CorrectiveAction { get; set; }
    public bool RegisterCorrected { get; set; }
    public string? ResolvedByName { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    /// <summary>Signed link valid for a short time (on-screen report only); null when there's no photo.</summary>
    public string? PhotoUrl { get; set; }
    public bool HasPhoto { get; set; }
}
