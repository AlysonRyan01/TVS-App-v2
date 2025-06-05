using Microsoft.EntityFrameworkCore;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;
using TVS_App.Infrastructure.Data;
using TVS_App.Infrastructure.Exceptions;

namespace TVS_App.Infrastructure.Repositories.Customers;

public class CustomerQueryRepository : ICustomerQueryRepository
{
    private readonly ApplicationDataContext _context;

    public CustomerQueryRepository(ApplicationDataContext context)
    {
        _context = context;
    }
    
    public async Task<BaseResponse<Customer?>> GetByIdAsync(long id)
    {
        try
        {
            if (id <= 0)
                return new BaseResponse<Customer?>(null, 400, "O ID não pode ser menor ou igual a 0");

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (customer == null)
                return new BaseResponse<Customer?>(null, 404, $"O cliente com id:{id} não existe");

            return new BaseResponse<Customer?>(customer, 200, "Cliente recuperado com sucesso!"); 
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<Customer?>(ex);
        }
    }

    public async Task<BaseResponse<List<Customer>>> GetCustomerByName(string name)
    {
        try
        {
            var customers = await _context.Customers
                .AsNoTracking()
                .Where(c => c.Name.CustomerName.StartsWith(name))
                .OrderBy(c => c.Name.CustomerName)
                .Take(10)
                .ToListAsync();

            return new BaseResponse<List<Customer>>(customers, 200, "Clientes obtidos com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<List<Customer>>(ex);
        }
    }

    public async Task<BaseResponse<PaginatedResult<Customer?>>> GetAllAsync(int pageNumber, int pageSize)
    {
        try
        {
            if (pageNumber < 1)
                return new BaseResponse<PaginatedResult<Customer?>>(null, 400, "O número da página deve ser maior que zero.");
    
            if (pageSize < 1)
                return new BaseResponse<PaginatedResult<Customer?>>(null, 400, "O tamanho da página deve ser maior que zero.");

            var totalCount = await _context.Customers.CountAsync();

            var customers = await _context.Customers
                .AsNoTracking()
                .OrderBy(c => c.Name.CustomerName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

            var result = new PaginatedResult<Customer?>(customers, totalCount, pageNumber, pageSize, totalPages);

            return new BaseResponse<PaginatedResult<Customer?>>(result, 200, "Clientes recuperados com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<PaginatedResult<Customer?>>(ex);
        }
    }
}