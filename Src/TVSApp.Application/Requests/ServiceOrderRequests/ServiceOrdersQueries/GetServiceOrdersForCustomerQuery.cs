using MediatR;
using TVS_App.Application.DTOs;
using TVS_App.Application.Exceptions;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;

public record GetServiceOrdersForCustomerQuery(long ServiceOrderId, string SecurityCode) 
    : IRequest<BaseResponse<EstimateServiceOrder>>
{
    public string NormalizedSecurityCode => SecurityCode.Trim().ToUpper();
    
    public void Validate()
    {
        if (string.IsNullOrEmpty(SecurityCode))
            throw new QueryException<GetServiceOrdersForCustomerQuery>("O código de segurança não pode ser vazio");
    }
}