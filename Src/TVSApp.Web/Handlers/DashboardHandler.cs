using System.Net.Http.Json;
using TVS_App.Application.DTOs.Dashboard;
using TVS_App.Domain.Shared;

namespace TVSApp.Web.Handlers;

public class DashboardHandler
{
    private readonly HttpClient _httpClient;

    public DashboardHandler(IHttpClientFactory clientFactory)
    {
        _httpClient = clientFactory.CreateClient("api");
    }

    public async Task<BaseResponse<DashboardMetricsDto>> GetDashboardMetricsAsync(int month, int year)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<DashboardMetricsDto>>(
                $"get-dashboard-metrics?month={month}&year={year}");

            if (response == null)
                return new BaseResponse<DashboardMetricsDto>(null, 500, "Resposta inválida do servidor.");

            return response;
        }
        catch (Exception ex)
        {
            return new BaseResponse<DashboardMetricsDto>(null, 500, $"Erro ao carregar métricas: {ex.Message}");
        }
    }

    public async Task<BaseResponse<DashboardMetricsDto>> GetDashboardMetricsByRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var start = startDate.ToString("yyyy-MM-dd");
            var end = endDate.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<DashboardMetricsDto>>(
                $"get-dashboard-metrics-by-range?startDate={start}&endDate={end}");

            if (response == null)
                return new BaseResponse<DashboardMetricsDto>(null, 500, "Resposta inválida do servidor.");

            return response;
        }
        catch (Exception ex)
        {
            return new BaseResponse<DashboardMetricsDto>(null, 500, $"Erro ao carregar métricas: {ex.Message}");
        }
    }
}
