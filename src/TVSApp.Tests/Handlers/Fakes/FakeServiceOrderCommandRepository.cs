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

    public ServiceOrder? LastCreated { get; private set; }

    /// <summary>Resposta forçada do CreateAsync. Nulo = sucesso (OS com Id 1 e o cliente de teste, como o repositório real).</summary>
    public BaseResponse<ServiceOrder?>? CreateResponse { get; set; }

    public Task<BaseResponse<ServiceOrder?>> CreateAsync(ServiceOrder serviceOrder)
    {
        LastCreated = serviceOrder;
        if (CreateResponse is not null)
            return Task.FromResult(CreateResponse);

        serviceOrder.Id = 1;
        serviceOrder.UpdateCustomer(ServiceOrderFactory.Create().Customer);
        return Task.FromResult(new BaseResponse<ServiceOrder?>(serviceOrder));
    }
}
