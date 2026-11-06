using TVS_App.Application.DTOs.Dashboard;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Metrics;

public interface IMetricsQueryHandler
{
    /// <summary>De+Até → intervalo; senão Mês/Ano (padrão: mês atual).</summary>
    Task<BaseResponse<DashboardMetricsDto>> GetDashboard(int? month, int? year, DateTime? from, DateTime? to);
}
