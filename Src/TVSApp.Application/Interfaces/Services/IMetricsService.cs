using TVS_App.Application.DTOs.Dashboard;

namespace TVS_App.Application.Interfaces.Services;

public interface IMetricsService
{
    Task<DashboardMetricsDto> GetDashboardMetricsAsync(int month, int year);
    Task<DashboardMetricsDto> GetDashboardMetricsByRangeAsync(DateTime startDate, DateTime endDate);
    Task<AbandonedOrdersDto> GetAbandonedOrdersAsync();
}
