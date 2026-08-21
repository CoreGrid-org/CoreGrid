using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Verification.DTOs;
using CoreGrid.Api.Features.Verification.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Verification.Controllers;

// FR-056: campaign creation is Auditor/Administrator only; read access is
// open to any authenticated org member (an assigned officer needs to see
// which campaign their task belongs to).
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
    public async Task<ActionResult<List<CampaignDto>>> GetCampaigns(CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        return Ok(await _campaignService.GetCampaignsAsync(currentUser.OrganizationId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CampaignDto>> GetCampaignById(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var campaign = await _campaignService.GetCampaignByIdAsync(currentUser.OrganizationId, id);
        if (campaign is null) return NotFound(new { message = "Campaign not found." });

        return Ok(campaign);
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<CampaignDto>> CreateCampaign(
        [FromBody] CreateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        try
        {
            var campaign = await _campaignService.CreateCampaignAsync(
                currentUser.OrganizationId,
                currentUser.Id,
                request);

            return CreatedAtAction(nameof(GetCampaignById), new { id = campaign.Id }, campaign);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // FR-065: campaign completion report — Auditor/Administrator, same as
    // creation, since generating one is itself an audit action.
    [HttpGet("{id:guid}/report")]
    [Authorize(Roles = $"{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<CampaignReportDto>> GetReport(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await _reportService.GetReportAsync(currentUser.OrganizationId, id, cancellationToken);
        if (report is null) return NotFound(new { message = "Campaign not found." });

        return Ok(report);
    }

    // FR-084/FR-085: same report, rendered as a downloadable PDF or CSV.
    [HttpGet("{id:guid}/report/export")]
    [Authorize(Roles = $"{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<IActionResult> ExportReport(Guid id, [FromQuery] string format, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var report = await _reportService.GetReportAsync(currentUser.OrganizationId, id, cancellationToken);
        if (report is null) return NotFound(new { message = "Campaign not found." });

        var fileNameStem = string.Join("-", report.CampaignName.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return format.ToLowerInvariant() switch
        {
            "csv" => File(_reportService.BuildCsv(report), "text/csv", $"{fileNameStem}-report.csv"),
            "pdf" => File(_reportService.BuildPdf(report), "application/pdf", $"{fileNameStem}-report.pdf"),
            _ => BadRequest(new { message = "Unsupported export format. Use 'pdf' or 'csv'." })
        };
    }
}
