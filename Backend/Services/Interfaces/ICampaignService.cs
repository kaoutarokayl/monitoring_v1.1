using KtcWeb.Application.DTOs;

namespace KtcWeb.Application.Interfaces
{
    public interface ICampaignService
    {
        Task<List<CampaignDto>> GetAllCampaignsAsync();
        Task<CampaignDto?> GetCampaignByIdAsync(int campaignId);
        Task<CampaignDto?> CreateCampaignAsync(CreateCampaignRequest request);
        Task<CampaignDto?> UpdateCampaignAsync(int campaignId, CreateCampaignRequest request);
        Task<bool> DeleteCampaignAsync(int campaignId);
        Task<List<CampaignBusinessDto>> GetCampaignBusinessesAsync(int campaignId);
        Task<List<CampaignGroupDto>> GetCampaignGroupsAsync(int campaignId);
        Task<List<CampaignBINRangeDto>> GetCampaignBINRangesAsync(int campaignId);
        Task<List<CampaignShownCountDto>> GetCampaignShownCountsAsync(int campaignId);

        Task<bool> GetGlobalMarketingEnabledAsync();
        Task<bool> SetGlobalMarketingEnabledAsync(bool enabled);
        Task<bool> GetBusinessMarketingEnabledAsync(int businessId);
        Task<bool> SetBusinessMarketingEnabledAsync(int businessId, bool enabled);
    }
}