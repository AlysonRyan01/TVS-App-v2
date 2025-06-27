using Microsoft.EntityFrameworkCore;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;
using TVS_App.Infrastructure.Data;

namespace TVS_App.Infrastructure.Repositories.ServiceOrders;

public class ServiceOrderQueryRepository : IServiceOrderQueryRepository
{
    private readonly ApplicationDataContext _context;

    public ServiceOrderQueryRepository(ApplicationDataContext context)
    {
        _context = context;
    }
    
    public async Task<BaseResponse<ServiceOrder?>> GetById(long id)
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

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetAllAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O número da página deve ser maior que zero.");
    
        if (pageSize < 1)
            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O tamanho da página deve ser maior que zero.");

        var totalCount = await _context.ServiceOrders.CountAsync();

        var serviceOrders = await _context.ServiceOrders
            .Include(x => x.Customer)
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

        var result = new PaginatedResult<ServiceOrder?>(serviceOrders, totalCount, pageNumber, pageSize, totalPages);

        return new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço retornadas com sucesso!");
    }

    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByCustomerName(string customerName)
    {
        var serviceOrders = await _context.ServiceOrders
            .Include(so => so.Customer)
            .AsNoTracking()
            .Where(so => so.Customer.Name.CustomerName.Contains(customerName))
            .OrderByDescending(so => so.Id)
            .ToListAsync();
            
        if (!serviceOrders.Any())
            return new BaseResponse<List<ServiceOrder>>(serviceOrders, 401, "Nenhuma ordem de serviço foi encontrada.");

        return new BaseResponse<List<ServiceOrder>>(serviceOrders, 200, $"{serviceOrders.Count} ordens obtidas com sucesso!");
    }

    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersBySerialNumber(string serialNumber)
    {
        var serviceOrders = await _context.ServiceOrders
            .Include(so => so.Customer)
            .AsNoTracking()
            .Where(so => so.Product.SerialNumber.Contains(serialNumber))
            .OrderByDescending(so => so.Id)
            .ToListAsync();
            
        if (!serviceOrders.Any())
            return new BaseResponse<List<ServiceOrder>>(serviceOrders, 401, "Nenhuma ordem de serviço foi encontrada.");
            
        return new BaseResponse<List<ServiceOrder>>(serviceOrders, 200, $"{serviceOrders.Count} ordens obtidas com sucesso!");
    }

    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByModel(string model)
    {
        var serviceOrders = await _context.ServiceOrders
            .Include(so => so.Customer)
            .AsNoTracking()
            .Where(so => so.Product.Model.Contains(model))
            .OrderByDescending(so => so.Id)
            .ToListAsync();
            
        if (!serviceOrders.Any())
            return new BaseResponse<List<ServiceOrder>>(serviceOrders, 401, "Nenhuma ordem de serviço foi encontrada.");
            
        return new BaseResponse<List<ServiceOrder>>(serviceOrders, 200, $"{serviceOrders.Count} ordens obtidas com sucesso!");
    }

    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByEnterprise(EEnterprise enterprise)
    {
        var serviceOrders = await _context.ServiceOrders
            .Include(so => so.Customer)
            .AsNoTracking()
            .Where(so => so.Enterprise == enterprise)
            .OrderByDescending(so => so.Id)
            .ToListAsync();
            
        if (!serviceOrders.Any())
            return new BaseResponse<List<ServiceOrder>>(serviceOrders, 401, "Nenhuma ordem de serviço foi encontrada.");
            
        return new BaseResponse<List<ServiceOrder>>(serviceOrders, 200, $"{serviceOrders.Count} ordens obtidas com sucesso!");
    }

    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByDate(DateTime startDate, DateTime endDate)
    {
        var serviceOrders = await _context.ServiceOrders
            .Include(so => so.Customer)
            .AsNoTracking()
            .Where(so => so.EntryDate >= startDate && so.EntryDate <= endDate)
            .OrderBy(so => so.Id)
            .ToListAsync();
            
        if (!serviceOrders.Any())
            return new BaseResponse<List<ServiceOrder>>(serviceOrders, 401, "Nenhuma ordem de serviço foi encontrada.");
            
        return new BaseResponse<List<ServiceOrder>>(serviceOrders, 200, $"{serviceOrders.Count} ordens obtidas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetPendingEstimatesAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O número da página deve ser maior que zero.");

        if (pageSize < 1)
            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O tamanho da página deve ser maior que zero.");

        var twoMonthAgo = DateTime.UtcNow.AddMonths(-2);

        var query = _context.ServiceOrders
            .Where(x => x.ServiceOrderStatus == EServiceOrderStatus.Entered &&
                        x.RepairStatus == ERepairStatus.Entered && x.EntryDate >= twoMonthAgo)
            .OrderByDescending(x => x.Id)
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var deliveredServiceOrders = await query
            .Include(x => x.Customer)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

        var result = new PaginatedResult<ServiceOrder?>(deliveredServiceOrders, totalCount, pageNumber, pageSize, totalPages);
        return new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço pendentes de orçamento recuperadas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetWaitingResponseAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O número da página deve ser maior que zero.");

        if (pageSize < 1)
            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O tamanho da página deve ser maior que zero.");

        var sixMonthAgo = DateTime.UtcNow.AddMonths(-6);

        var query = _context.ServiceOrders
            .Where(x => x.ServiceOrderStatus == EServiceOrderStatus.Evaluated &&
                        x.RepairStatus == ERepairStatus.Waiting && x.InspectionDate >= sixMonthAgo && (x.RepairResult != ERepairResult.Unrepaired || x.RepairResult == ERepairResult.NoDefectFound ))
            .OrderByDescending(x => x.Id)
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var waitingResponseServiceOrders = await query
            .Include(x => x.Customer)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

        var result = new PaginatedResult<ServiceOrder?>(waitingResponseServiceOrders, totalCount, pageNumber, pageSize, totalPages);
        return new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço aguardando resposta do cliente recuperadas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetPendingPartPurchase(int pageNumber, int pageSize)
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
            .OrderByDescending(x => x.Id)
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var pendingPartPurchaseServiceOrders = await query
            .Include(x => x.Customer)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

        var result = new PaginatedResult<ServiceOrder?>(pendingPartPurchaseServiceOrders, totalCount, pageNumber, pageSize, totalPages);
        return new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço pendentes de compra de peça recuperadas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetWaitingPartsAsync(int pageNumber, int pageSize)
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
            .OrderByDescending(x => x.Id)
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var waitingPartsServiceOrders = await query
            .Include(x => x.Customer)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

        var result = new PaginatedResult<ServiceOrder?>(waitingPartsServiceOrders, totalCount, pageNumber, pageSize, totalPages);
        return new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço aguardando peça recuperadas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetWaitingPickupAsync(int pageNumber, int pageSize)
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
            .OrderByDescending(x => x.Id)
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var waitingPickUpServiceOrders = await query
            .Include(x => x.Customer)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

        var result = new PaginatedResult<ServiceOrder?>(waitingPickUpServiceOrders, totalCount, pageNumber, pageSize, totalPages);
        return new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço aguardando coleta recuperadas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetDeliveredAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O número da página deve ser maior que zero.");

        if (pageSize < 1)
            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 400, "O tamanho da página deve ser maior que zero.");

        var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

        var query = _context.ServiceOrders
            .Where(x => x.ServiceOrderStatus == EServiceOrderStatus.Delivered && x.DeliveryDate >= oneMonthAgo)
            .OrderByDescending(x => x.Id)
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var deliveredServiceOrders = await query
            .Include(x => x.Customer)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

        var result = new PaginatedResult<ServiceOrder?>(deliveredServiceOrders, totalCount, pageNumber, pageSize, totalPages);
        return new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço entregues recuperadas com sucesso!");
    }
}