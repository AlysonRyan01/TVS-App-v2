using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Exceptions;
using TVS_App.Application.Requests.CustomerRequests.CustomersQueries;
using TVS_App.Domain.Repositories;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;

public record GetPendingEstimatesQuery(int PageNumber, int PageSize) 
    : IRequest<BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>>
{
    public void Validate()
    {
        if (PageNumber < 1)
            throw new QueryException<GetAllCustomersQuery>("O pageNumber não pode ser menor que 1");

        if (PageSize < 1)
            throw new QueryException<GetAllCustomersQuery>("O pageSize não pode ser menor que 1");
    }
}