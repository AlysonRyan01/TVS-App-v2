using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVSApp.Tests.Handlers.Fakes;

public class FakeServiceOrderCommandRepository : IServiceOrderCommandRepository
{
    public ServiceOrder? LastUpdated { get; private set; }

    /// <summary>Resposta forçada do UpdateAsync (ex.: falha ao salvar). Nulo = sucesso.</summary>
    public BaseResponse<ServiceOrder?>? UpdateResponse { get; set; }

    public Task<BaseResponse<ServiceOrder?>> UpdateAsync(ServiceOrder serviceOrder)
    {
        LastUpdated = serviceOrder;
        return Task.FromResult(UpdateResponse ?? new BaseResponse<ServiceOrder?>(serviceOrder));
    }

    public Task<BaseResponse<ServiceOrder?>> CreateAsync(ServiceOrder serviceOrder) => throw new NotImplementedException();
}
