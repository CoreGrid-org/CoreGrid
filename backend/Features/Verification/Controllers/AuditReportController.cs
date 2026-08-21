using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Verification.DTOs;
using CoreGrid.Api.Features.Verification.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Verification.Controllers;

// FR-084/FR-085: the "Audit Campaign Report" tab on the shared Reports page
// — Auditor/Administrator only, matching every other export in this
// feature. Distinct from VerificationCampaignsController's per-campaign
// report (FR-065).
[ApiController]
[Route("api/reports/audit")]
[Authorize(Roles = $"{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}")]
public class AuditReportController : CoreGridControllerBase
{
    private readonly IAuditReportService _reportService;

    public AuditReportController(IAuditReportService reportService, CoreGridDbContext db) : base(db)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<ActionResult<AuditReportDto>> GetReport(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var filter = new AuditReportFilter { From = from, To = to, DepartmentId = departmentId, AssetCategoryId = categoryId, Status = status };
        var report = await _reportService.GetReportAsync(currentUser.OrganizationId, filter, cancellationToken);

        return Ok(report);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportReport(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? status,
        [FromQuery] string format,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var filter = new AuditReportFilter { From = from, To = to, DepartmentId = departmentId, AssetCategoryId = categoryId, Status = status };
        var report = await _reportService.GetReportAsync(currentUser.OrganizationId, filter, cancellationToken);

        return format.ToLowerInvariant() switch
        {
            "csv" => File(_reportService.BuildCsv(report), "text/csv", "audit-report.csv"),
            "pdf" => File(_reportService.BuildPdf(report), "application/pdf", "audit-report.pdf"),
            _ => BadRequest(new { message = "Unsupported export format. Use 'pdf' or 'csv'." })
        };
    }
}
