using TVS_App.Application.DTOs.Dashboard;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Metrics;

public interface IMetricsQueryHandler
{
    Task<BaseResponse<DashboardMetricsDto>> GetDashboard(int? month, int? year, DateTime? from, DateTime? to);
}
