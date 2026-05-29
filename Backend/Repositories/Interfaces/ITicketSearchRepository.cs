using System.Collections.Generic;
using System.Threading.Tasks;
using KtcWeb.Application.DTOs;

namespace KtcWeb.Domain.Interfaces
{
    public interface ITicketSearchRepository
    {
        Task<List<TicketSearchResultDto>> SearchTicketsAsync(TicketSearchCriteriaDto criteria);
        Task<List<TicketTypeLookupDto>> GetTicketTypesAsync();
        Task<List<ErrorCodeLookupDto>> GetErrorCodesAsync();
    }
}
