using MediatR;
using TVS_App.Application.DTOs;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class GetServiceOrderForCustomerHandler 
    : IRequestHandler<GetServiceOrdersForCustomerQuery, BaseResponse<EstimateServiceOrder>>
{
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public GetServiceOrderForCustomerHandler(IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }

    public async Task<BaseResponse<EstimateServiceOrder>> Handle(
        GetServiceOrdersForCustomerQuery request, 
        CancellationToken cancellationToken = default)
    {
        var orderResult = await _serviceOrderQueryRepository.GetById(request.ServiceOrderId);
        if (!orderResult.IsSuccess)
            return new BaseResponse<EstimateServiceOrder>(null, 404, "Nenhuma ordem de serviço encontrada");

        var serviceOrder = orderResult.Data;

        if (!string.Equals(serviceOrder?.SecurityCode, request.NormalizedSecurityCode))
            return new BaseResponse<EstimateServiceOrder>(null, 404, "O código de segurança está incorreto");
            
        if (serviceOrder == null)
            return new BaseResponse<EstimateServiceOrder>(null, 404, "Ocorreu um erro ao buscar a ordem de serviço");
            
        var dto = new EstimateServiceOrder(
            serviceOrder.Id, 
            serviceOrder.Customer.Name.CustomerName,
            serviceOrder.Product.Type,
            serviceOrder.Product.Brand, 
            serviceOrder.Product.Model,
            serviceOrder.Product.Defect ?? "",
            serviceOrder.Guarantee?.ServiceOrderGuarantee,
            serviceOrder.TotalAmount, serviceOrder.EstimateMessage,
            serviceOrder.ServiceOrderStatus,
            serviceOrder.RepairResult,
            serviceOrder.RepairStatus,
            serviceOrder.Enterprise);

        return new BaseResponse<EstimateServiceOrder>(dto, 200, "Ordem de serviço obtida com sucesso!");
    }
}