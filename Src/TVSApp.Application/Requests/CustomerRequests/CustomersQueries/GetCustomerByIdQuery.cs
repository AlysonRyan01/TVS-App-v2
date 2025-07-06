using MediatR;
using TVS_App.Application.DTOs;
using TVS_App.Application.Exceptions;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.CustomerRequests.CustomersQueries;

public record GetCustomerByIdQuery(long Id) : IRequest<BaseResponse<CustomerDto?>>
{
    public void Validate()
    {
        if (Id == 0)
            throw new QueryException<GetCustomerByIdQuery>("O Id do cliente não pode ser 0");
    }
}