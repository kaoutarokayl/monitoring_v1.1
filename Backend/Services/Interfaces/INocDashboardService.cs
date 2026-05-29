using KtcWeb.Application.DTOs;

namespace KtcWeb.Application.Interfaces
{
    public interface INocDashboardService
    {
        Task<NocSummaryDto> GetNocSummaryAsync(DateTime? from = null, DateTime? to = null);
    }
}
