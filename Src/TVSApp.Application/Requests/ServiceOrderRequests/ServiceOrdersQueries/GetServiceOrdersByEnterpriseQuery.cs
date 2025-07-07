using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;

public record GetServiceOrdersByEnterpriseQuery(EEnterprise Enterprise) : IRequest<BaseResponse<List<ServiceOrderDto>>>;