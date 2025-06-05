using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;
using TVS_App.Infrastructure.Data;
using TVS_App.Infrastructure.Exceptions;

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
        IDbContextTransaction? transaction = null;
        
        try
        {
            transaction = await _context.Database.BeginTransactionAsync();

            await _context.ServiceOrders.AddAsync(serviceOrder);
            await _context.SaveChangesAsync();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == serviceOrder.CustomerId);

            if (customer == null)
                return new BaseResponse<ServiceOrder?>(null, 404, "Cliente não encontrado");

            serviceOrder.UpdateCustomer(customer);

            await transaction.CommitAsync();

            return new BaseResponse<ServiceOrder?>(serviceOrder, 200, "Ordem de serviço criada com sucesso!");
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync();

            return DbExceptionHandler.Handle<ServiceOrder?>(ex);
        }
    }

    public async Task<BaseResponse<ServiceOrder?>> UpdateAsync(ServiceOrder serviceOrder)
    {
        IDbContextTransaction? transaction = null;
        
        try
        {
            transaction = await _context.Database.BeginTransactionAsync();

            _context.ServiceOrders.Update(serviceOrder);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new BaseResponse<ServiceOrder?>(serviceOrder, 200, "Ordem de serviço atualizada com sucesso!");
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync();

            return DbExceptionHandler.Handle<ServiceOrder?>(ex);
        }
    }
}