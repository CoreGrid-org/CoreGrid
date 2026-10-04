namespace CoreGrid.Api.Features.Maintenance.DTOs;

// GET /api/maintenance/{id}/cost-suggestion — a data-driven starting point for
// the estimated cost an approver records. SuggestedCost is null when there is
// no comparable history yet; the approver then enters the figure manually.
public class MaintenanceCostSuggestionDto
{
    public decimal? SuggestedCost { get; set; }
    public decimal? LowCost { get; set; }
    public decimal? HighCost { get; set; }
    public int SampleSize { get; set; }

    // ASSET | ASSET_TYPE | CATEGORY | ORGANIZATION | NONE — which slice of
    // history the suggestion was drawn from (most specific with enough data).
    public required string Basis { get; set; }
    public required string BasisLabel { get; set; }
    public bool PriorityMatched { get; set; }

    public required string Confidence { get; set; } // HIGH | MEDIUM | LOW | NONE
    public required string Method { get; set; }
}
