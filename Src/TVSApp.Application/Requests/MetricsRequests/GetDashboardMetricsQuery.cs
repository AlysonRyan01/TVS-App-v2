using MediatR;
using TVS_App.Application.DTOs.Dashboard;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.MetricsRequests;

public record GetDashboardMetricsQuery : IRequest<BaseResponse<DashboardMetricsDto>>;
