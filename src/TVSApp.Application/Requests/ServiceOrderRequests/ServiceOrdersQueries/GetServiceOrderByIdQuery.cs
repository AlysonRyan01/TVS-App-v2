using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Exceptions;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;

public record GetServiceOrderByIdQuery(long Id) : IRequest<BaseResponse<ServiceOrderDto?>>
{
    public void Validate()
    {
        if (Id == 0)
            throw new QueryException<GetServiceOrderByIdQuery>("O Id da ordem de serviço não pode ser 0");
    }
}
