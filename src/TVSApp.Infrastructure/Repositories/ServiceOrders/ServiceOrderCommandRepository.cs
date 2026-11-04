using Microsoft.EntityFrameworkCore;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;
using TVS_App.Infrastructure.Data;

namespace TVS_App.Infrastructure.Repositories.ServiceOrders;

public class ServiceOrderCommandRepository : IServiceOrderCommandRepository
{
    private readonly ApplicationDataContext _context;

    public ServiceOrderCommandRepository(ApplicationDataContext context)
    {
        _context = context;
    }
    
    public async Task<BaseResponse<ServiceOrder?>> CreateAsync(ServiceOrder serviceOrder)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == serviceOrder.CustomerId);

        if (customer == null)
            return new BaseResponse<ServiceOrder?>(null, 404, "Cliente não encontrado");

        serviceOrder.UpdateCustomer(customer);

        await _context.ServiceOrders.AddAsync(serviceOrder);
        await _context.SaveChangesAsync();
        
        await transaction.CommitAsync();

        return new BaseResponse<ServiceOrder?>(serviceOrder, 200, "Ordem de serviço criada com sucesso!");
    }

    public async Task<BaseResponse<ServiceOrder?>> UpdateAsync(ServiceOrder serviceOrder)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        _context.ServiceOrders.Update(serviceOrder);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return new BaseResponse<ServiceOrder?>(serviceOrder, 200, "Ordem de serviço atualizada com sucesso!");
    }
}