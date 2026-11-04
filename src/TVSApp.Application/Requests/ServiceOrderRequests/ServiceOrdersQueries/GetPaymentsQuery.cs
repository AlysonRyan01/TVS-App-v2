using MediatR;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;

public record GetPaymentsQuery(long ServiceOrderId) : IRequest<BaseResponse<List<Payment>>>;