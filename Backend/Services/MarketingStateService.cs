namespace KtcWeb.Application.Services
{
    /// <summary>
    /// Service singleton pour gérer l'état du marketing en mémoire (sans BD).
    /// </summary>
    public class MarketingStateService
    {
        private bool _globalMarketingEnabled = true;
        private readonly Dictionary<int, bool> _businessMarketingState = new();
        private readonly Dictionary<int, bool> _campaignMarketingState = new();
        private readonly Dictionary<(int campaignId, int businessId), bool> _campaignBusinessMarketingState = new();
        private readonly object _lock = new();

        public Task<bool> GetGlobalMarketingEnabledAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_globalMarketingEnabled);
            }
        }

        public Task<bool> SetGlobalMarketingEnabledAsync(bool enabled)
        {
            lock (_lock)
            {
                _globalMarketingEnabled = enabled;
                return Task.FromResult(enabled);
            }
        }

        public Task<bool> GetBusinessMarketingEnabledAsync(int businessId)
        {
            lock (_lock)
            {
                if (_businessMarketingState.TryGetValue(businessId, out var enabled))
                {
                    return Task.FromResult(enabled);
                }
                // Par défaut, retourner true si le business n'a pas d'état spécifique
                return Task.FromResult(true);
            }
        }

        public Task<bool> SetBusinessMarketingEnabledAsync(int businessId, bool enabled)
        {
            lock (_lock)
            {
                _businessMarketingState[businessId] = enabled;
                return Task.FromResult(enabled);
            }
        }

        public Task<bool> GetCampaignMarketingEnabledAsync(int campaignId)
        {
            lock (_lock)
            {
                if (_campaignMarketingState.TryGetValue(campaignId, out var enabled))
                {
                    return Task.FromResult(enabled);
                }
                // Par défaut, retourner true si la campagne n'a pas d'état spécifique
                return Task.FromResult(true);
            }
        }

        public Task<bool> SetCampaignMarketingEnabledAsync(int campaignId, bool enabled)
        {
            lock (_lock)
            {
                _campaignMarketingState[campaignId] = enabled;
                return Task.FromResult(enabled);
            }
        }

        public Task<bool> GetCampaignBusinessMarketingEnabledAsync(int campaignId, int businessId)
        {
            lock (_lock)
            {
                var key = (campaignId, businessId);
                if (_campaignBusinessMarketingState.TryGetValue(key, out var enabled))
                {
                    return Task.FromResult(enabled);
                }
                // Par défaut, retourner true si la combinaison campagne-business n'a pas d'état spécifique
                return Task.FromResult(true);
            }
        }

        public Task<bool> SetCampaignBusinessMarketingEnabledAsync(int campaignId, int businessId, bool enabled)
        {
            lock (_lock)
            {
                var key = (campaignId, businessId);
                _campaignBusinessMarketingState[key] = enabled;
                return Task.FromResult(enabled);
            }
        }
    }
}
