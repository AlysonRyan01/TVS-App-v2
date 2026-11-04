using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class EditServiceOrderHandler : IRequestHandler<EditServiceOrderCommand, BaseResponse<UpdateServiceOrderResponseDto?>>
{
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public EditServiceOrderHandler(
        IServiceOrderCommandRepository serviceOrderCommandRepository, 
        IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }

    public async Task<BaseResponse<UpdateServiceOrderResponseDto?>> Handle(
        EditServiceOrderCommand request, 
        CancellationToken cancellationToken = default)
    {
        request.Validate();
        var normalized = request.Normalize();
            
        if (normalized.Id == 0)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(
                null, 
                401, 
                "O ID da ordem de serviço não pode ser 0");
            
        var repositoryResult = await _serviceOrderQueryRepository.GetById(normalized.Id);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 500, repositoryResult.Message);
            
        var serviceOrder = repositoryResult.Data;
            
        ServiceOrderMapper.ApplyChanges(serviceOrder, normalized);
            
        var result = await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);
        if (!result.IsSuccess || result.Data == null)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 500, result.Message);
            
        var dto = ServiceOrderMapper.MapToUpdateResponseDto(result.Data);
            
        return new BaseResponse<UpdateServiceOrderResponseDto?>(dto, 200, result.Message);
    }
}