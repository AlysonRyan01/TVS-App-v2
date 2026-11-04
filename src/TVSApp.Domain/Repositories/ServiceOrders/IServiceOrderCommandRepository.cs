using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVS_App.Domain.Repositories.ServiceOrders;

public interface IServiceOrderCommandRepository
{
    Task<BaseResponse<ServiceOrder?>> CreateAsync(ServiceOrder serviceOrder);
    Task<BaseResponse<ServiceOrder?>> UpdateAsync(ServiceOrder serviceOrder);
}