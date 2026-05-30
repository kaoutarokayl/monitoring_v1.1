using KtcWeb.Application.DTOs;
using KtcWeb.Application.Interfaces;
using KtcWeb.Domain.Interfaces;

namespace KtcWeb.Application.Services
{
    public class TicketSearchService : ITicketSearchService
    {
        private readonly ITicketSearchRepository _repository;

        public TicketSearchService(ITicketSearchRepository repository)
        {
            _repository = repository;
        }

        public Task<List<TicketTypeLookupDto>> GetTicketTypesAsync()
            => _repository.GetTicketTypesAsync();

        public Task<List<ErrorCodeLookupDto>> GetErrorCodesAsync()
            => _repository.GetErrorCodesAsync();

        public Task<List<TicketSearchResultDto>> SearchTicketsAsync(TicketSearchCriteriaDto criteria)
            => _repository.SearchTicketsAsync(criteria);
    }
}