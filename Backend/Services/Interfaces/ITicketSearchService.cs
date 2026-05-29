using System.Collections.Generic;
using System.Threading.Tasks;
using KtcWeb.Application.DTOs;

namespace KtcWeb.Application.Interfaces
{
    public interface ITicketSearchService
    {
        Task<List<TicketSearchResultDto>> SearchTicketsAsync(TicketSearchCriteriaDto criteria);
        Task<List<TicketTypeLookupDto>> GetTicketTypesAsync();
        Task<List<ErrorCodeLookupDto>> GetErrorCodesAsync();
    }
}
