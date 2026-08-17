using System.Collections.Generic;

namespace CoreGrid.Api.Domain;

public class DisposalPreconditionResult
{
    public bool SeparationOfDutiesPassed { get; set; } = true;
    public string? SeparationOfDutiesFailureReason { get; set; }
    
    public bool AllPassed { get; set; }
    public List<PreconditionCheck> Checks { get; set; } = new();
}

public class PreconditionCheck
{
    public string Code { get; set; } = string.Empty;        // "P1".."P6"
    public string Description { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? FailureReason { get; set; }
}
