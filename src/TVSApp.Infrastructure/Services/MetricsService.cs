using Microsoft.EntityFrameworkCore;
using TVS_App.Application.DTOs.Dashboard;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Services;
using TVS_App.Domain.Enums;
using TVS_App.Infrastructure.Data;

namespace TVS_App.Infrastructure.Services;

public class MetricsService : IMetricsService
{
    private readonly ApplicationDataContext _context;

    public MetricsService(ApplicationDataContext context)
    {
        _context = context;
    }

    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(int month, int year)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        var orders = await _context.ServiceOrders
            .AsNoTracking()
            .Where(x => x.EntryDate >= start && x.EntryDate < end)
            .ToListAsync();

        var deliveredOrders = await _context.ServiceOrders
            .Include(x => x.Customer)
            .AsNoTracking()
            .Where(x => x.DeliveryDate.HasValue && x.DeliveryDate >= start && x.DeliveryDate < end)
            .ToListAsync();

        return DashboardMetricsCalculator.Build(orders, deliveredOrders);
    }

    public async Task<DashboardMetricsDto> GetDashboardMetricsByRangeAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1);

        var orders = await _context.ServiceOrders
            .AsNoTracking()
            .Where(x => x.EntryDate >= start && x.EntryDate < end)
            .ToListAsync();

        var deliveredOrders = await _context.ServiceOrders
            .Include(x => x.Customer)
            .AsNoTracking()
            .Where(x => x.DeliveryDate.HasValue && x.DeliveryDate >= start && x.DeliveryDate < end)
            .ToListAsync();

        return DashboardMetricsCalculator.Build(orders, deliveredOrders);
    }

}
