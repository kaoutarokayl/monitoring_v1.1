using KtcWeb.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KtcWeb.API.Controllers
{
    [ApiController]
    [Route("api/noc")]
    public class NocDashboardController(INocDashboardService service) : ControllerBase
    {
        [HttpGet("summary")]
        public async Task<IActionResult> GetNocSummary(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to   = null)
        {
            var result = await service.GetNocSummaryAsync(from, to);
            return Ok(result);
        }
    }
}
