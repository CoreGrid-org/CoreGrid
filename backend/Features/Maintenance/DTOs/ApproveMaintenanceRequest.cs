using System;

namespace CoreGrid.Api.Features.Maintenance.DTOs;

/// <summary>
/// Request body for FR-036: an Inventory Officer or Administrator approves a
/// REQUESTED maintenance record, assigning it to a responsible officer and
/// recording an estimated cost.
/// </summary>
public class ApproveMaintenanceRequest
{
    /// <summary>
    /// The user who will be responsible for carrying out the work.
    /// Must belong to the same organisation and hold at least the
    /// InventoryOfficer role.
    /// </summary>
    public Guid AssigneeId { get; set; }

    /// <summary>
    /// Non-negative estimated cost for the work (used as the budget
    /// baseline and for the BR1 variance check on completion).
    /// </summary>
    public decimal EstimatedCost { get; set; }
}
