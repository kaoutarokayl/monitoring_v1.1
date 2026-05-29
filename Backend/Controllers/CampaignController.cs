using KtcWeb.Application.DTOs;
using KtcWeb.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KtcWeb.API.Controllers
{
    [ApiController]
    [Route("api/campaign")]
    [Authorize(Policy = "RequireReadOnly")]
    public class CampaignController(ICampaignService campaignService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<CampaignDto>>> GetAllCampaigns()
        {
            var campaigns = await campaignService.GetAllCampaignsAsync();
            return Ok(campaigns);
        }

        [HttpGet("{campaignId}")]
        public async Task<ActionResult<CampaignDto>> GetCampaignById(int campaignId)
        {
            var campaign = await campaignService.GetCampaignByIdAsync(campaignId);
            if (campaign == null)
                return NotFound(new { error = $"Campagne {campaignId} non trouvée" });

            return Ok(campaign);
        }

        [HttpPost]
        public async Task<ActionResult<CampaignDto>> CreateCampaign([FromBody] CreateCampaignRequest request)
        {
            var created = await campaignService.CreateCampaignAsync(request);
            return Ok(new { status = "Campagne créée avec succès", campaign = created });
        }

        [HttpPut("{campaignId}")]
        public async Task<ActionResult<CampaignDto>> UpdateCampaign(int campaignId, [FromBody] CreateCampaignRequest request)
        {
            var updated = await campaignService.UpdateCampaignAsync(campaignId, request);
            if (updated == null)
                return NotFound(new { error = $"Campagne {campaignId} non trouvée" });

            return Ok(updated);
        }

        [HttpDelete("{campaignId}")]
        public async Task<ActionResult> DeleteCampaign(int campaignId)
        {
            var deleted = await campaignService.DeleteCampaignAsync(campaignId);
            if (!deleted)
                return NotFound(new { error = $"Campagne {campaignId} non trouvée" });

            return Ok(new { status = "Campagne supprimée avec succès" });
        }

        [HttpGet("{campaignId}/businesses")]
        public async Task<ActionResult<List<CampaignBusinessDto>>> GetCampaignBusinesses(int campaignId)
        {
            var businesses = await campaignService.GetCampaignBusinessesAsync(campaignId);
            return Ok(businesses);
        }

        [HttpGet("{campaignId}/groups")]
        public async Task<ActionResult<List<CampaignGroupDto>>> GetCampaignGroups(int campaignId)
        {
            var groups = await campaignService.GetCampaignGroupsAsync(campaignId);
            return Ok(groups);
        }

        [HttpGet("{campaignId}/bin-ranges")]
        public async Task<ActionResult<List<CampaignBINRangeDto>>> GetCampaignBINRanges(int campaignId)
        {
            var binRanges = await campaignService.GetCampaignBINRangesAsync(campaignId);
            return Ok(binRanges);
        }

        [HttpGet("{campaignId}/shown-counts")]
        public async Task<ActionResult<List<CampaignShownCountDto>>> GetCampaignShownCounts(int campaignId)
        {
            var counts = await campaignService.GetCampaignShownCountsAsync(campaignId);
            return Ok(counts);
        }

        // ── Marketing Control ───────────────────────────────────────────────────
        [HttpGet("marketing/global")]
        public async Task<ActionResult<object>> GetGlobalMarketingState()
        {
            var enabled = await campaignService.GetGlobalMarketingEnabledAsync();
            return Ok(new { enabled });
        }

        [HttpPost("marketing/global")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<ActionResult<object>> SetGlobalMarketingState([FromBody] SetMarketingStateRequest request)
        {
            var result = await campaignService.SetGlobalMarketingEnabledAsync(request.Enabled);
            return Ok(new { enabled = result });
        }

        [HttpGet("marketing/business/{businessId}")]
        public async Task<ActionResult<object>> GetBusinessMarketingState(int businessId)
        {
            var enabled = await campaignService.GetBusinessMarketingEnabledAsync(businessId);
            return Ok(new { enabled });
        }

        [HttpPost("marketing/business/{businessId}")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<ActionResult<object>> SetBusinessMarketingState(int businessId, [FromBody] SetMarketingStateRequest request)
        {
            var result = await campaignService.SetBusinessMarketingEnabledAsync(businessId, request.Enabled);
            return Ok(new { enabled = result });
        }
    }
}
