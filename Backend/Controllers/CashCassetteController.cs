using KtcWeb.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KtcWeb.API.Controllers
{
    [ApiController]
    [Route("api/atm")]
    public class CashCassetteController(ICashCassetteService service) : ControllerBase
    {
        [HttpGet("clients/{clientId}/cash-units")]
        public async Task<ActionResult> GetCashUnitStatus(int clientId, [FromQuery] short? componentId = null)
        {
            var result = await service.GetCashUnitStatusAsync(clientId, componentId);
            return Ok(result);
        }

        [HttpGet("clients/{clientId}/cash-summary")]
        public async Task<ActionResult> GetCashUnitSummary(int clientId)
        {
            var result = await service.GetCashUnitSummaryAsync(clientId);
            return Ok(result);
        }

        [HttpGet("clients/{clientId}/cash-flow")]
        public async Task<ActionResult> GetCashFlowReport(
            int clientId,
            [FromQuery] short componentId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to   = null)
        {
            if (componentId == 0)
                return BadRequest(new { message = "Component ID is required" });

            var result = await service.GetCashFlowReportAsync(clientId, componentId, from, to);
            if (result == null)
                return NotFound(new { message = "No cash flow data found for this component" });

            return Ok(result);
        }

        [HttpGet("clients/{clientId}/cash-units-history")]
        public async Task<ActionResult> GetCashUnitHistory(
            int clientId,
            [FromQuery] short? componentId = null,
            [FromQuery] DateTime? from     = null,
            [FromQuery] DateTime? to       = null,
            [FromQuery] int? limit         = 500)
        {
            if (limit.HasValue && limit.Value <= 0)
                return BadRequest(new { message = "limit must be greater than 0" });

            var result = await service.GetCashUnitHistoryAsync(clientId, componentId, from, to, limit);
            return Ok(result);
        }

        [HttpGet("clients/{clientId}/cassettes")]
        public async Task<ActionResult> GetPhysicalCassettes(int clientId, [FromQuery] short? componentId = null)
        {
            var result = await service.GetPhysicalCassettesAsync(clientId, componentId);
            return Ok(result);
        }

        [HttpGet("clients/{clientId}/cassettes-summary")]
        public async Task<ActionResult> GetCassetteSummary(int clientId)
        {
            var result = await service.GetCassetteSummaryAsync(clientId);
            return Ok(result);
        }

        [HttpGet("cassettes/{cassetteId}/status")]
        public async Task<ActionResult> GetCassetteStatusReport(long cassetteId)
        {
            var result = await service.GetCassetteStatusReportAsync(cassetteId);
            if (result == null)
                return NotFound(new { message = "Cassette not found" });

            return Ok(result);
        }

        [HttpGet("clients/{clientId}/cassettes-status")]
        public async Task<ActionResult> GetCassetteStatusReportByClient(int clientId)
        {
            var result = await service.GetCassetteStatusReportByClientAsync(clientId);
            return Ok(result);
        }

        [HttpGet("clients/{clientId}/cash-cassette-overview")]
        public async Task<ActionResult> GetAtmCashCassetteOverview(int clientId)
        {
            var result = await service.GetAtmCashCassetteOverviewAsync(clientId);
            if (result == null)
                return NotFound(new { message = "ATM not found" });

            return Ok(result);
        }

        [HttpGet("lookups/cash-unit-statuses")]
        public async Task<ActionResult> GetCashUnitStatuses()
        {
            var result = await service.GetCashUnitStatusesAsync();
            return Ok(result);
        }

        [HttpGet("lookups/cash-unit-types")]
        public async Task<ActionResult> GetCashUnitTypes()
        {
            var result = await service.GetCashUnitTypesAsync();
            return Ok(result);
        }
    }
}
