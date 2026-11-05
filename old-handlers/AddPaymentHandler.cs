using MediatR;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class AddPaymentHandler : IRequestHandler<AddPaymentCommand, BaseResponse<string>>
{
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public AddPaymentHandler(
        IServiceOrderCommandRepository serviceOrderCommandRepository, 
        IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }

    public async Task<BaseResponse<string>> Handle(AddPaymentCommand request, CancellationToken cancellationToken = default)
    {
        var serviceOrderResult = await _serviceOrderQueryRepository.GetByIdForUpdate(request.ServiceOrderId);
        if (!serviceOrderResult.IsSuccess || serviceOrderResult.Data == null)
            return new BaseResponse<string>(serviceOrderResult.Message);

        var serviceOrder = serviceOrderResult.Data;

        var payment = new Payment(request.Amount, request.PaymentDate, request.ServiceOrderId);
        serviceOrder.AddPayment(payment);

        var updateResult = await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);
        return new BaseResponse<string>(updateResult.Message);
    }
}