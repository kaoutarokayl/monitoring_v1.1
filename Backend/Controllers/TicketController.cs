using KtcWeb.Application.DTOs;
using KtcWeb.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KtcWeb.API.Controllers
{
    [ApiController]
    [Route("api/ticket")]
    [Authorize(Policy = "RequireReadOnly")]
    public class TicketController(ITicketSearchService ticketSearchService) : ControllerBase
    {
        /// <summary>
        /// Get all ticket types for dropdown/filter
        /// </summary>
        [HttpGet("types")]
        public async Task<ActionResult<List<TicketTypeLookupDto>>> GetTicketTypes()
        {
            var ticketTypes = await ticketSearchService.GetTicketTypesAsync();
            return Ok(ticketTypes);
        }

        /// <summary>
        /// Get all error codes for dropdown/filter
        /// </summary>
        [HttpGet("error-codes")]
        public async Task<ActionResult<List<ErrorCodeLookupDto>>> GetErrorCodes()
        {
            var errorCodes = await ticketSearchService.GetErrorCodesAsync();
            return Ok(errorCodes);
        }

        /// <summary>
        /// Search tickets with criteria
        /// </summary>
        [HttpPost("search")]
        public async Task<ActionResult<List<TicketSearchResultDto>>> SearchTickets([FromBody] TicketSearchCriteriaDto criteria)
        {
            if (criteria == null)
                return BadRequest(new { error = "Criteria cannot be null" });

            var results = await ticketSearchService.SearchTicketsAsync(criteria);
            return Ok(results);
        }
    }
}
