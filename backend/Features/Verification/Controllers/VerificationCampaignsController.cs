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

    public VerificationCampaignsController(
        IVerificationCampaignService campaignService,
        CoreGridDbContext db) : base(db)
    {
        _campaignService = campaignService;
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
}
