using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Exceptions;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;

public record GetServiceOrdersByDateQuery(DateTime StartDate, DateTime EndDate) 
    : IRequest<BaseResponse<List<ServiceOrderDto>>>
{
    public void Validate()
    {
        if (StartDate > EndDate)
            throw new QueryException<GetServiceOrdersByDateQuery>(
                "A data de início não pode ser maior que a data de término.");
    }
};