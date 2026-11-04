using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;

public record GetServiceOrdersByCustomerNameQuery(string Name) : IRequest<BaseResponse<List<ServiceOrderDto>>>;