using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Verification.DTOs;
using CoreGrid.Api.Features.Verification.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Verification.Controllers;

// FR-056: campaign creation is Auditor/Administrator only (CanManageCampaigns);
// read access is open to any authenticated org member (an assigned officer
// needs to see which campaign their task belongs to).
[ApiController]
[Route("api/verification-campaigns")]
[Authorize]
public class VerificationCampaignsController : CoreGridControllerBase
{
    private readonly IVerificationCampaignService _campaignService;
    private readonly ICampaignReportService _reportService;

    public VerificationCampaignsController(
        IVerificationCampaignService campaignService,
        ICampaignReportService reportService,
        CoreGridDbContext db) : base(db)
    {
        _campaignService = campaignService;
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CampaignDto>>> GetCampaigns(
        [FromQuery] CampaignQueryParameters query,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        return Ok(await _campaignService.GetCampaignsAsync(currentUser.OrganizationId, query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CampaignDto>> GetCampaignById(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var campaign = await _campaignService.GetCampaignByIdAsync(currentUser.OrganizationId, id, cancellationToken);
        return campaign is null
            ? throw NotFoundException.For(nameof(VerificationCampaign), id)
            : Ok(campaign);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageCampaigns)]
    public async Task<ActionResult<CampaignDto>> CreateCampaign(
        [FromBody] CreateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var campaign = await _campaignService.CreateCampaignAsync(
            currentUser.OrganizationId, currentUser.Id, request, cancellationToken);

        return CreatedAtAction(nameof(GetCampaignById), new { id = campaign.Id }, campaign);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageCampaigns)]
    public async Task<ActionResult<CampaignDto>> UpdateCampaign(
        Guid id,
        [FromBody] UpdateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var updated = await _campaignService.UpdateCampaignAsync(currentUser.OrganizationId, id, request, cancellationToken);

        return updated is null
            ? throw NotFoundException.For(nameof(VerificationCampaign), id)
            : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanManageCampaigns)]
    public async Task<IActionResult> DeleteCampaign(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var success = await _campaignService.DeleteCampaignAsync(currentUser.OrganizationId, id, cancellationToken);

        if (!success)
        {
            throw NotFoundException.For(nameof(VerificationCampaign), id);
        }

        return NoContent();
    }

    // FR-065: campaign completion report — Auditor/Administrator, same as
    // creation, since generating one is itself an audit action.
    [HttpGet("{id:guid}/report")]
    [Authorize(Policy = Policies.CanManageCampaigns)]
    public async Task<ActionResult<CampaignReportDto>> GetReport(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await _reportService.GetReportAsync(currentUser.OrganizationId, id, cancellationToken);
        return report is null
            ? throw NotFoundException.For(nameof(VerificationCampaign), id)
            : Ok(report);
    }

    // FR-084/FR-085: same report, rendered as a downloadable PDF or CSV.
    [HttpGet("{id:guid}/report/export")]
    [Authorize(Policy = Policies.CanManageCampaigns)]
    public async Task<IActionResult> ExportReport(Guid id, [FromQuery] string format, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await _reportService.GetReportAsync(currentUser.OrganizationId, id, cancellationToken);
        if (report is null)
        {
            throw NotFoundException.For(nameof(VerificationCampaign), id);
        }

        var fileNameStem = string.Join("-", report.CampaignName.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return format.ToLowerInvariant() switch
        {
            "csv" => File(_reportService.BuildCsv(report), "text/csv", $"{fileNameStem}-report.csv"),
            "pdf" => File(_reportService.BuildPdf(report), "application/pdf", $"{fileNameStem}-report.pdf"),
            _ => throw new ValidationException(nameof(format), "Unsupported export format. Use 'pdf' or 'csv'.")
        };
    }
}
