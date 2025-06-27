using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;
using TVS_App.Infrastructure.Data;

namespace TVS_App.Infrastructure.Repositories.Customers;

public class CustomerCommandRepository : ICustomerCommandRepository
{
    private readonly ApplicationDataContext _context;

    public CustomerCommandRepository(ApplicationDataContext context)
    {
        _context = context;
    }
    
    public async Task<BaseResponse<Customer>> CreateAsync(Customer customer)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        await _context.Customers.AddAsync(customer);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return new BaseResponse<Customer>(customer, 200, "Cliente criado com sucesso!");
    }

    public async Task<BaseResponse<Customer?>> UpdateAsync(Customer customer)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return new BaseResponse<Customer?>(customer, 200, "Cliente atualizado com sucesso!");
    }
}