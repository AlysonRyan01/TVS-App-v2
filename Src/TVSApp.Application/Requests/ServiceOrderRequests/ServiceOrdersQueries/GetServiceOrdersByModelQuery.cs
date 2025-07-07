using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Exceptions;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;

public record GetServiceOrdersByModelQuery(string Model) : IRequest<BaseResponse<List<ServiceOrderDto>>>
{
    public string NormalizedModel => Model.Trim().ToUpper();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Model))
            throw new QueryException<GetServiceOrdersByModelQuery>("O modelo não pode ser vazio.");
    }
}