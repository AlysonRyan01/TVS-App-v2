using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Queries.ServiceOrdersQueries;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class RegenerateAndReturnPdfHandler : ICommandHandler<GetServiceOrderByIdQuery, BaseResponse<byte[]>>
{
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;
    private readonly IGenerateServiceOrderPdf _generateServiceOrderPdf;

    public RegenerateAndReturnPdfHandler(
        IServiceOrderQueryRepository serviceOrderQueryRepository,
        IGenerateServiceOrderPdf generateServiceOrderPdf)
    {
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
        _generateServiceOrderPdf = generateServiceOrderPdf;
    }
    
    public async Task<BaseResponse<byte[]>> HandleAsync(GetServiceOrderByIdQuery command)
    {
        command.Validate();

        var result = await _serviceOrderQueryRepository.GetById(command.Id);
        if (result.Data == null)
            return new BaseResponse<byte[]>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        var createPdf = await _generateServiceOrderPdf.RegeneratePdfAsync(serviceOrder);
        if (!createPdf.IsSuccess)
            return new BaseResponse<byte[]>(null, 500, createPdf.Message);

        return createPdf;
    }
}