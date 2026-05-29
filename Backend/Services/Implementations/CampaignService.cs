using KtcWeb.Application.DTOs;
using KtcWeb.Application.Interfaces;
using KtcWeb.Domain.Interfaces;

namespace KtcWeb.Application.Services
{
    public class CampaignService : ICampaignService
    {
        private readonly ICampaignRepository _repo;
        private readonly MarketingStateService _marketingStateService;

        public CampaignService(ICampaignRepository repo, MarketingStateService marketingStateService)
        {
            _repo = repo;
            _marketingStateService = marketingStateService;
        }

        public Task<List<CampaignDto>> GetAllCampaignsAsync() =>
            _repo.GetAllCampaignsAsync();

        public Task<CampaignDto?> GetCampaignByIdAsync(int campaignId) =>
            _repo.GetCampaignByIdAsync(campaignId);

        public async Task<CampaignDto?> CreateCampaignAsync(CreateCampaignRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new InvalidOperationException("Le nom de la campagne est obligatoire");

            // 1. Insérer la campagne dans Campaigns
            await _repo.CreateCampaignAsync(request);

            // 2. Récupérer la campagne qui vient d'être créée (par nom, la plus récente)
            CampaignDto? created = null;
            try
            {
                created = await _repo.GetCampaignByNameLatestAsync(request.Name);
            }
            catch
            {
                // Problème de synchronisation DB temporaire — on retourne null
                return null;
            }

            // 3. Lier les businesses si des IDs ont été fournis
            if (created != null && request.BusinessIds?.Any() == true)
            {
                await _repo.SetCampaignBusinessesAsync(created.CampaignId, request.BusinessIds);
            }

            return created;
        }

        public async Task<CampaignDto?> UpdateCampaignAsync(int campaignId, CreateCampaignRequest request)
        {
            // 1. Mettre à jour les champs de la campagne
            await _repo.UpdateCampaignAsync(campaignId, request);

            // 2. Mettre à jour les businesses associés si la propriété a été fournie.
            //    Si la liste est vide, on efface toutes les associations existantes.
            if (request.BusinessIds != null)
            {
                await _repo.SetCampaignBusinessesAsync(campaignId, request.BusinessIds);
            }

            // 3. Retourner la campagne mise à jour
            try
            {
                return await _repo.GetCampaignByIdAsync(campaignId);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteCampaignAsync(int campaignId)
        {
            var rows = await _repo.DeleteCampaignAsync(campaignId);
            return rows > 0;
        }

        public Task<List<CampaignBusinessDto>> GetCampaignBusinessesAsync(int campaignId) =>
            _repo.GetCampaignBusinessesAsync(campaignId);

        public Task<List<CampaignGroupDto>> GetCampaignGroupsAsync(int campaignId) =>
            _repo.GetCampaignGroupsAsync(campaignId);

        public Task<List<CampaignBINRangeDto>> GetCampaignBINRangesAsync(int campaignId) =>
            _repo.GetCampaignBINRangesAsync(campaignId);

        public Task<List<CampaignShownCountDto>> GetCampaignShownCountsAsync(int campaignId) =>
            _repo.GetCampaignShownCountsAsync(campaignId);

        public Task<bool> GetGlobalMarketingEnabledAsync() =>
            _marketingStateService.GetGlobalMarketingEnabledAsync();

        public Task<bool> SetGlobalMarketingEnabledAsync(bool enabled) =>
            _marketingStateService.SetGlobalMarketingEnabledAsync(enabled);

        public Task<bool> GetBusinessMarketingEnabledAsync(int businessId) =>
            _marketingStateService.GetBusinessMarketingEnabledAsync(businessId);

        public Task<bool> SetBusinessMarketingEnabledAsync(int businessId, bool enabled) =>
            _marketingStateService.SetBusinessMarketingEnabledAsync(businessId, enabled);

        public Task<bool> GetCampaignMarketingEnabledAsync(int campaignId) =>
            _marketingStateService.GetCampaignMarketingEnabledAsync(campaignId);

        public Task<bool> SetCampaignMarketingEnabledAsync(int campaignId, bool enabled) =>
            _marketingStateService.SetCampaignMarketingEnabledAsync(campaignId, enabled);

        public Task<bool> GetCampaignBusinessMarketingEnabledAsync(int campaignId, int businessId) =>
            _marketingStateService.GetCampaignBusinessMarketingEnabledAsync(campaignId, businessId);

        public Task<bool> SetCampaignBusinessMarketingEnabledAsync(int campaignId, int businessId, bool enabled) =>
            _marketingStateService.SetCampaignBusinessMarketingEnabledAsync(campaignId, businessId, enabled);
    }
}