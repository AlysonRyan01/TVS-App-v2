using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TVS_App.Application.DTOs;
using TVS_App.Application.Repositories;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Infrastructure.Data;
using TVS_App.Infrastructure.Exceptions;

namespace TVS_App.Infrastructure.Repositories;

public class ServiceOrderRepository : IServiceOrderRepository
{
    private readonly ApplicationDataContext _context;

    public ServiceOrderRepository(ApplicationDataContext context)
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

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetAllAsync(int pageNumber, int pageSize)
    {
        try
        {
            if (pageNumber < 1)
                return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O número da página deve ser maior que zero.");
    
            if (pageSize < 1)
                return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O tamanho da página deve ser maior que zero.");

            var totalCount = await _context.ServiceOrders.CountAsync();

            var serviceOrders = await _context.ServiceOrders
                .AsNoTracking()
                .Include(x => x.Customer)
                .OrderByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

            var result = new PaginatedResult<ServiceOrder?>(serviceOrders, totalCount, pageNumber, pageSize, totalPages);

            return new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço retornadas com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<PaginatedResult<ServiceOrder?>>(ex);
        }
    }

    public async Task<BaseResponse<ServiceOrder?>> GetById(long id)
    {
        try
        {
            if (id <= 0)
                return new BaseResponse<ServiceOrder?>(null, 400, "O ID não pode ser igual ou menor que 0");

            var serviceOrder = await _context.ServiceOrders
                .Include(x => x.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (serviceOrder == null || serviceOrder.Id == 0)
                return new BaseResponse<ServiceOrder?>(null, 404, $"A ordem de serviço com ID:{id} não existe");

            return new BaseResponse<ServiceOrder?>(serviceOrder, 200, "Ordem de serviço recuperada com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<ServiceOrder?>(ex);
        }
    }

    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByCustomerName(string customerName)
    {
        try
        {
            var serviceOrders = await _context.ServiceOrders
                .Include(so => so.Customer)
                .Where(so => so.Customer.Name.CustomerName.Contains(customerName))
                .OrderByDescending(so => so.Id)
                .ToListAsync();
            
            if (!serviceOrders.Any())
                return new BaseResponse<List<ServiceOrder>>(serviceOrders, 401, "Nenhuma ordem de serviço foi encontrada.");

            return new BaseResponse<List<ServiceOrder>>(serviceOrders, 200, $"{serviceOrders.Count} ordens obtidas com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<List<ServiceOrder>>(ex);
        }
    }

    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersBySerialNumber(string serialNumber)
    {
        try
        {
            var serviceOrders = await _context.ServiceOrders
                .Include(so => so.Customer)
                .Where(so => so.Product.SerialNumber.Contains(serialNumber))
                .OrderByDescending(so => so.Id)
                .ToListAsync();
            
            if (!serviceOrders.Any())
                return new BaseResponse<List<ServiceOrder>>(serviceOrders, 401, "Nenhuma ordem de serviço foi encontrada.");
            
            return new BaseResponse<List<ServiceOrder>>(serviceOrders, 200, $"{serviceOrders.Count} ordens obtidas com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<List<ServiceOrder>>(ex);
        }
    }

    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByModel(string model)
    {
        try
        {
            var serviceOrders = await _context.ServiceOrders
                .Include(so => so.Customer)
                .Where(so => so.Product.Model.Contains(model))
                .OrderByDescending(so => so.Id)
                .ToListAsync();
            
            if (!serviceOrders.Any())
                return new BaseResponse<List<ServiceOrder>>(serviceOrders, 401, "Nenhuma ordem de serviço foi encontrada.");
            
            return new BaseResponse<List<ServiceOrder>>(serviceOrders, 200, $"{serviceOrders.Count} ordens obtidas com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<List<ServiceOrder>>(ex);
        }
    }

    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByEnterprise(EEnterprise enterprise)
    {
        try
        {
            var serviceOrders = await _context.ServiceOrders
                .Include(so => so.Customer)
                .Where(so => so.Enterprise == enterprise)
                .OrderByDescending(so => so.Id)
                .ToListAsync();
            
            if (!serviceOrders.Any())
                return new BaseResponse<List<ServiceOrder>>(serviceOrders, 401, "Nenhuma ordem de serviço foi encontrada.");
            
            return new BaseResponse<List<ServiceOrder>>(serviceOrders, 200, $"{serviceOrders.Count} ordens obtidas com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<List<ServiceOrder>>(ex);
        }
    }

    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByDate(DateTime startDate, DateTime endDate)
    {
        try
        {
            var serviceOrders = await _context.ServiceOrders
                    .Include(so => so.Customer)
                    .Where(so => so.EntryDate >= startDate && so.EntryDate <= endDate)
                    .OrderBy(so => so.Id)
                    .ToListAsync();
            
            if (!serviceOrders.Any())
                return new BaseResponse<List<ServiceOrder>>(serviceOrders, 401, "Nenhuma ordem de serviço foi encontrada.");
            
            return new BaseResponse<List<ServiceOrder>>(serviceOrders, 200, $"{serviceOrders.Count} ordens obtidas com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<List<ServiceOrder>>(ex);
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetDeliveredAsync(int pageNumber, int pageSize)
    {
        try
        {
            if (pageNumber < 1)
                return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O número da página deve ser maior que zero.");

            if (pageSize < 1)
                return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O tamanho da página deve ser maior que zero.");

            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

            var query = _context.ServiceOrders
                .Where(x => x.ServiceOrderStatus == EServiceOrderStatus.Delivered && x.DeliveryDate >= oneMonthAgo)
                .OrderBy(x => x.Id);

            var totalCount = await query.CountAsync();

            var deliveredServiceOrders = await query
                .Include(x => x.Customer)
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

            var result = new PaginatedResult<ServiceOrder?>(deliveredServiceOrders, totalCount, pageNumber, pageSize, totalPages);
            return new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço entregues recuperadas com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<PaginatedResult<ServiceOrder?>>(ex);
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetPendingEstimatesAsync(int pageNumber, int pageSize)
    {
        try
        {
            if (pageNumber < 1)
                return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O número da página deve ser maior que zero.");

            if (pageSize < 1)
                return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O tamanho da página deve ser maior que zero.");

            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

            var query = _context.ServiceOrders
                .Where(x => x.ServiceOrderStatus == EServiceOrderStatus.Entered &&
                    x.RepairStatus == ERepairStatus.Entered && x.EntryDate >= oneMonthAgo)
                .OrderBy(x => x.Id);

            var totalCount = await query.CountAsync();

            var deliveredServiceOrders = await query
                .Include(x => x.Customer)
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

            var result = new PaginatedResult<ServiceOrder?>(deliveredServiceOrders, totalCount, pageNumber, pageSize, totalPages);
            return new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço pendentes de orçamento recuperadas com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<PaginatedResult<ServiceOrder?>>(ex);
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetPendingPartPurchase(int pageNumber, int pageSize)
    {
        try
        {
            if (pageNumber < 1)
                return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O número da página deve ser maior que zero.");

            if (pageSize < 1)
                return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O tamanho da página deve ser maior que zero.");

            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

            var query = _context.ServiceOrders
                .Where(x => x.ServiceOrderStatus == EServiceOrderStatus.Evaluated &&
                    x.RepairStatus == ERepairStatus.Approved &&
                    !x.PurchasePartDate.HasValue &&
                    x.ResponseDate >= oneMonthAgo)
                .OrderBy(x => x.Id);

            var totalCount = await query.CountAsync();

            var deliveredServiceOrders = await query
                .Include(x => x.Customer)
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

            var result = new PaginatedResult<ServiceOrder?>(deliveredServiceOrders, totalCount, pageNumber, pageSize, totalPages);
            return new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço pendentes de compra de peça recuperadas com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<PaginatedResult<ServiceOrder?>>(ex);
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetWaitingPartsAsync(int pageNumber, int pageSize)
    {
        try
        {
            if (pageNumber < 1)
                return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O número da página deve ser maior que zero.");

            if (pageSize < 1)
                return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O tamanho da página deve ser maior que zero.");

            var treeMonthAgo = DateTime.UtcNow.AddMonths(-3);

            var query = _context.ServiceOrders
                .Where(x => x.ServiceOrderStatus == EServiceOrderStatus.OrderPart &&
                    x.RepairStatus == ERepairStatus.Approved &&
                    x.PurchasePartDate.HasValue &&
                    x.PurchasePartDate >= treeMonthAgo)
                .OrderBy(x => x.Id);

            var totalCount = await query.CountAsync();

            var deliveredServiceOrders = await query
                .Include(x => x.Customer)
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

            var result = new PaginatedResult<ServiceOrder?>(deliveredServiceOrders, totalCount, pageNumber, pageSize, totalPages);
            return new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço aguardando peça recuperadas com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<PaginatedResult<ServiceOrder?>>(ex);
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetWaitingPickupAsync(int pageNumber, int pageSize)
    {
        try
        {
            if (pageNumber < 1)
                return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O número da página deve ser maior que zero.");

            if (pageSize < 1)
                return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O tamanho da página deve ser maior que zero.");

            var sixMonths = DateTime.UtcNow.AddMonths(-6);

            var query = _context.ServiceOrders
                .Where(x => (x.RepairDate >= sixMonths || x.RepairDate == null) && (x.ServiceOrderStatus == EServiceOrderStatus.Repaired ||
                                                            (x.ServiceOrderStatus == EServiceOrderStatus.Evaluated &&
                                                             (x.RepairStatus == ERepairStatus.Disapproved || x.RepairResult == ERepairResult.Unrepaired || x.RepairResult == ERepairResult.NoDefectFound))))
                .OrderBy(x => x.Id);

            var totalCount = await query.CountAsync();

            var deliveredServiceOrders = await query
                .Include(x => x.Customer)
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

            var result = new PaginatedResult<ServiceOrder?>(deliveredServiceOrders, totalCount, pageNumber, pageSize, totalPages);
            return new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço aguardando coleta recuperadas com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<PaginatedResult<ServiceOrder?>>(ex);
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetWaitingResponseAsync(int pageNumber, int pageSize)
    {
        try
        {
            if (pageNumber < 1)
                return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O número da página deve ser maior que zero.");

            if (pageSize < 1)
                return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O tamanho da página deve ser maior que zero.");

            var sixMonthAgo = DateTime.UtcNow.AddMonths(-6);

            var query = _context.ServiceOrders
                .Where(x => x.ServiceOrderStatus == EServiceOrderStatus.Evaluated &&
                    x.RepairStatus == ERepairStatus.Waiting && x.InspectionDate >= sixMonthAgo && (x.RepairResult != ERepairResult.Unrepaired || x.RepairResult == ERepairResult.NoDefectFound ))
                .OrderBy(x => x.Id);

            var totalCount = await query.CountAsync();

            var deliveredServiceOrders = await query
                .Include(x => x.Customer)
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

            var result = new PaginatedResult<ServiceOrder?>(deliveredServiceOrders, totalCount, pageNumber, pageSize, totalPages);
            return new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço aguardando resposta do cliente recuperadas com sucesso!");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<PaginatedResult<ServiceOrder?>>(ex);
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