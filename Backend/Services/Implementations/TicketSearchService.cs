using System.Collections.Generic;
using System.Threading.Tasks;
using KtcWeb.Application.DTOs;
using KtcWeb.Application.Interfaces;
using KtcWeb.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace KtcWeb.Application.Services
{
    public class TicketSearchService : ITicketSearchService
    {
        private readonly ITicketSearchRepository _repository;
        private readonly ILogger<TicketSearchService> _logger;

        public TicketSearchService(ITicketSearchRepository repository, ILogger<TicketSearchService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<TicketSearchResultDto>> SearchTicketsAsync(TicketSearchCriteriaDto criteria)
        {
            try
            {
                return await _repository.SearchTicketsAsync(criteria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search tickets");
                return new List<TicketSearchResultDto>();
            }
        }

        public async Task<List<TicketTypeLookupDto>> GetTicketTypesAsync()
        {
            try
            {
                return await _repository.GetTicketTypesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load ticket types");
                return new List<TicketTypeLookupDto>();
            }
        }

        public async Task<List<ErrorCodeLookupDto>> GetErrorCodesAsync()
        {
            try
            {
                return await _repository.GetErrorCodesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load error codes");
                return new List<ErrorCodeLookupDto>();
            }
        }
    }
}
