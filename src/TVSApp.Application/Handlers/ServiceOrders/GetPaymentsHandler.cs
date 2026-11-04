using MediatR;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class GetPaymentsHandler : IRequestHandler<GetPaymentsQuery, BaseResponse<List<Payment>>>
{
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public GetPaymentsHandler(IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }

    public async Task<BaseResponse<List<Payment>>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        var serviceOrderResult = await _serviceOrderQueryRepository.GetById(request.ServiceOrderId);
        if (!serviceOrderResult.IsSuccess || serviceOrderResult.Data == null)
            return new BaseResponse<List<Payment>>(null, 404, serviceOrderResult.Message);

        var serviceOrder = serviceOrderResult.Data;
        
        return new BaseResponse<List<Payment>>(serviceOrder.Payments);
    }
}