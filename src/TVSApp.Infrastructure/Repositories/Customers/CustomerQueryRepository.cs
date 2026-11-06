using Microsoft.EntityFrameworkCore;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;
using TVS_App.Infrastructure.Data;

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
        if (id <= 0)
            return new BaseResponse<Customer?>(null, 400, "O ID não pode ser menor ou igual a 0");

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
        if (customer == null)
            return new BaseResponse<Customer?>(null, 404, $"O cliente com id:{id} não existe");

        return new BaseResponse<Customer?>(customer, 200, "Cliente recuperado com sucesso!"); 
    }

    public async Task<BaseResponse<List<Customer>>> GetCustomerByName(string name, int limit)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new BaseResponse<List<Customer>>(new List<Customer>(), 400, "O nome não pode ser vazio.");
        
        var searchTerm = name.Trim().ToUpper();

        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => c.Name.CustomerName.Contains(searchTerm))
            .OrderByDescending(c => c.Id)
            .Take(limit)
            .ToListAsync();

        return new BaseResponse<List<Customer>>(customers, 200, "Clientes obtidos com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<Customer>>> SearchAsync(CustomerSearchFilter filter, int pageNumber, int pageSize)
    {
        var query = _context.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            // Sem diferenciar maiúsculas nem acentos ("joao" encontra "JOÃO").
            var term = filter.Name.Trim().ToUpper();
            query = query.Where(c => EF.Functions.Collate(c.Name.CustomerName, "Latin1_General_CI_AI").Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.PhoneDigits))
        {
            // Telefones são gravados como digitados (com ou sem pontuação): compara só os dígitos.
            var digits = filter.PhoneDigits;
            query = query.Where(c =>
                c.Phone.CustomerPhone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "").Replace(".", "").Replace("+", "").Contains(digits)
                || c.Phone2.CustomerPhone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "").Replace(".", "").Replace("+", "").Contains(digits));
        }

        if (!string.IsNullOrWhiteSpace(filter.DocumentDigits))
        {
            var document = filter.DocumentDigits;
            query = query.Where(c => c.Document != null && c.Document.Number.Contains(document));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var page = new PaginatedResult<Customer>(items, total, pageNumber, pageSize,
            (int)Math.Ceiling(total / (double)pageSize));
        return new BaseResponse<PaginatedResult<Customer>>(page, 200, "Clientes obtidos com sucesso!");
    }
}
