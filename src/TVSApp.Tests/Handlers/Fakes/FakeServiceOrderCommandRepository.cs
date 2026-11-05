using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVSApp.Tests.Handlers.Fakes;

public class FakeServiceOrderCommandRepository : IServiceOrderCommandRepository
{
    public ServiceOrder? LastUpdated { get; private set; }

    public Task<BaseResponse<ServiceOrder?>> UpdateAsync(ServiceOrder serviceOrder)
    {
        LastUpdated = serviceOrder;
        return Task.FromResult(new BaseResponse<ServiceOrder?>(serviceOrder));
    }

    public Task<BaseResponse<ServiceOrder?>> CreateAsync(ServiceOrder serviceOrder) => throw new NotImplementedException();
}
