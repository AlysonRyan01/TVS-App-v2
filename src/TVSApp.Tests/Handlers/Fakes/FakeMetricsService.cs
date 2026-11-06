using TVS_App.Application.DTOs.Dashboard;
using TVS_App.Application.Interfaces.Services;

namespace TVSApp.Tests.Handlers.Fakes;

public class FakeMetricsService : IMetricsService
{
    public (int Month, int Year)? LastMonth { get; private set; }
    public (DateTime Start, DateTime End)? LastRange { get; private set; }

    public Task<DashboardMetricsDto> GetDashboardMetricsAsync(int month, int year)
    {
        LastMonth = (month, year);
        return Task.FromResult(new DashboardMetricsDto());
    }

    public Task<DashboardMetricsDto> GetDashboardMetricsByRangeAsync(DateTime startDate, DateTime endDate)
    {
        LastRange = (startDate, endDate);
        return Task.FromResult(new DashboardMetricsDto());
    }

}
