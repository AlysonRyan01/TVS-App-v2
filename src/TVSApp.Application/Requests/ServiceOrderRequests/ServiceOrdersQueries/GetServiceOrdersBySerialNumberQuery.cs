using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Exceptions;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;

public record GetServiceOrdersBySerialNumberQuery(string SerialNumber) : IRequest<BaseResponse<List<ServiceOrderDto>>>
{
    public string NormalizedSerialNumber => SerialNumber.Trim().ToUpper();
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SerialNumber))
            throw new QueryException<GetServiceOrdersBySerialNumberQuery>("O número de série não pode ser vazio.");
    }
}