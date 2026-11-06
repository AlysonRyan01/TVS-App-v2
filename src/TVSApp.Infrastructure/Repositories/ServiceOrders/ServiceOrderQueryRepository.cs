using Microsoft.EntityFrameworkCore;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Repositories;
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
    
    public async Task<BaseResponse<ServiceOrder?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return new BaseResponse<ServiceOrder?>(null, 400, "O ID não pode ser igual ou menor que 0");

        var serviceOrder = await _context.ServiceOrders
            .Include(x => x.Customer)
            .Include(x => x.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
        if (serviceOrder == null || serviceOrder.Id == 0)
            return new BaseResponse<ServiceOrder?>(null, 404, $"A ordem de serviço com ID:{id} não existe");

        return new BaseResponse<ServiceOrder?>(serviceOrder, 200, "Ordem de serviço recuperada com sucesso!");
    }

    public async Task<BaseResponse<ServiceOrder?>> GetByIdForUpdateAsync(long id)
    {
        if (id <= 0)
            return new BaseResponse<ServiceOrder?>(null, 400, "O ID não pode ser igual ou menor que 0");

        var serviceOrder = await _context.ServiceOrders
            .Include(x => x.Customer)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (serviceOrder == null || serviceOrder.Id == 0)
            return new BaseResponse<ServiceOrder?>(null, 404, $"A ordem de serviço com ID:{id} não existe");

        return new BaseResponse<ServiceOrder?>(serviceOrder, 200, "Ordem de serviço recuperada com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrderSearchItem>>> SearchAsync(
        ServiceOrderSearchFilter filter, int pageNumber, int pageSize)
    {
        var query = _context.ServiceOrders.AsNoTracking();

        if (filter.Number.HasValue)
            query = query.Where(x => x.Id == filter.Number.Value);

        if (filter.Customer != null)
            query = query.Where(x => EF.Functions.Like(x.Customer.Name.CustomerName, $"%{filter.Customer}%"));

        if (filter.Model != null)
            query = query.Where(x => EF.Functions.Like(x.Product.Model, $"%{filter.Model}%"));

        if (filter.SerialNumber != null)
            query = query.Where(x => EF.Functions.Like(x.Product.SerialNumber, $"%{filter.SerialNumber}%"));

        if (filter.From.HasValue)
            query = query.Where(x => x.EntryDate >= filter.From.Value);

        if (filter.To.HasValue)
        {
            var endExclusive = filter.To.Value.AddDays(1);
            query = query.Where(x => x.EntryDate < endExclusive);
        }

        var projected = query
            .OrderByDescending(x => x.Id)
            .Select(x => new ServiceOrderSearchItem(
                x.Id,
                x.Customer.Name.CustomerName,
                x.Product.Type,
                x.Product.Brand,
                x.Product.Model,
                x.Product.SerialNumber,
                x.Enterprise,
                x.ServiceOrderStatus,
                x.Product.Defect == "VENDA",
                x.EntryDate));

        var totalCount = await projected.CountAsync();
        var items = await projected
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

        return new BaseResponse<PaginatedResult<ServiceOrderSearchItem>>(
            new PaginatedResult<ServiceOrderSearchItem>(items, totalCount, pageNumber, pageSize, totalPages));
    }

    public async Task<BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>> GetPendingEstimateAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            return new BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>(null, 400, "O número da página deve ser maior que zero.");

        if (pageSize < 1)
            return new BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>(null, 400, "O tamanho da página deve ser maior que zero.");

        var fourMonthAgo = DateTime.UtcNow.AddMonths(-4);

        var query = _context.ServiceOrders
            .AsNoTracking()
            .Where(x => x.ServiceOrderStatus == EServiceOrderStatus.Entered &&
                        x.RepairStatus == ERepairStatus.Entered && x.EntryDate >= fourMonthAgo)
            .OrderByDescending(x => x.Id)
            .Select(so => new PendingEstimateServiceOrder(so.Id, so.Customer.Name.CustomerName, so.Enterprise.ToString(), so.Product.Model, so.Product.Defect, so.EntryDate));

        var totalCount = await query.CountAsync();

        var pendingEstimateServiceOrders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

        var result = new PaginatedResult<PendingEstimateServiceOrder>(pendingEstimateServiceOrders, totalCount, pageNumber, pageSize, totalPages);
        return new BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>(result, 200, "Ordens de serviço pendentes de orçamento recuperadas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<AwaitingResponseServiceOrder>>> GetAwaitingResponseAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            return new BaseResponse<PaginatedResult<AwaitingResponseServiceOrder>>(null, 400, "O número da página deve ser maior que zero.");

        if (pageSize < 1)
            return new BaseResponse<PaginatedResult<AwaitingResponseServiceOrder>>(null, 400, "O tamanho da página deve ser maior que zero.");

        var sixMonthAgo = DateTime.UtcNow.AddMonths(-6);

        var query = _context.ServiceOrders
            .AsNoTracking()
            .Where(x => x.ServiceOrderStatus == EServiceOrderStatus.Evaluated &&
                        x.RepairStatus == ERepairStatus.Waiting && x.InspectionDate >= sixMonthAgo &&
                        (x.RepairResult != ERepairResult.Unrepaired || x.RepairResult == ERepairResult.NoDefectFound))
            .OrderByDescending(x => x.Id)
            .Select(so => new AwaitingResponseServiceOrder(so.Id, so.Customer.Name.CustomerName, so.Product.Model, 
                so.Solution != null ? so.Solution.ServiceOrderSolution : null, so.TotalAmount, so.InspectionDate));

        var totalCount = await query.CountAsync();

        var awaitingResponseServiceOrders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

        var result = new PaginatedResult<AwaitingResponseServiceOrder>(awaitingResponseServiceOrders, totalCount, pageNumber, pageSize, totalPages);
        return new BaseResponse<PaginatedResult<AwaitingResponseServiceOrder>>(result, 200, "Ordens de serviço aguardando resposta do cliente recuperadas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ApprovedServiceOrder>>> GetApprovedAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            return new BaseResponse<PaginatedResult<ApprovedServiceOrder>>(null, 400, "O número da página deve ser maior que zero.");

        if (pageSize < 1)
            return new BaseResponse<PaginatedResult<ApprovedServiceOrder>>(null, 400, "O tamanho da página deve ser maior que zero.");

        var sixMountsAgo = DateTime.UtcNow.AddMonths(-6);

        var query = _context.ServiceOrders
            .AsNoTracking()
            .Where(x => x.ServiceOrderStatus == EServiceOrderStatus.Evaluated &&
                        x.RepairStatus == ERepairStatus.Approved &&
                        !x.PurchasePartDate.HasValue &&
                        x.ResponseDate >= sixMountsAgo)
            .OrderByDescending(x => x.Id)
            .Select(so => new ApprovedServiceOrder(so.Id, so.Enterprise.ToString(), so.Customer.Name.CustomerName, so.Product.Model, so.Solution != null ? so.Solution.ServiceOrderSolution : null, so.ResponseDate));

        var totalCount = await query.CountAsync();

        var approvedServiceOrders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

        var result = new PaginatedResult<ApprovedServiceOrder>(approvedServiceOrders, totalCount, pageNumber, pageSize, totalPages);
        return new BaseResponse<PaginatedResult<ApprovedServiceOrder>>(result, 200, "Ordens de serviço pendentes de compra de peça recuperadas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<RejectedServiceOrder>>> GetRejectedAsync(
        int pageNumber,
        int pageSize)
    {
        if (pageNumber < 1)
            return new BaseResponse<PaginatedResult<RejectedServiceOrder>>(
                null, 400, "O número da página deve ser maior que zero.");

        if (pageSize < 1)
            return new BaseResponse<PaginatedResult<RejectedServiceOrder>>(
                null, 400, "O tamanho da página deve ser maior que zero.");

        var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

        var query = _context.ServiceOrders
            .AsNoTracking()
            .Where(x => x.ServiceOrderStatus == EServiceOrderStatus.Evaluated &&
                        x.RepairStatus == ERepairStatus.Disapproved &&
                        x.ResponseDate >= oneMonthAgo)
            .OrderByDescending(x => x.Id)
            .Select(so => new RejectedServiceOrder(so.Id, so.Customer.Name.CustomerName, so.Product.Model, so.Solution != null ? so.Solution.ServiceOrderSolution : null, so.ResponseDate));

        var totalCount = await query.CountAsync();

        var rejectedServiceOrders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

        var result = new PaginatedResult<RejectedServiceOrder>(
            rejectedServiceOrders, totalCount, pageNumber, pageSize, totalPages);
        
        return new BaseResponse<PaginatedResult<RejectedServiceOrder>>(
            result, 200, "Ordens de serviço com orçamento rejeitado recuperadas com sucesso!");
    }
    
    public async Task<BaseResponse<PaginatedResult<AwaitingPartServiceOrder>>> GetAwaitingPartAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            return new BaseResponse<PaginatedResult<AwaitingPartServiceOrder>>(null, 400, "O número da página deve ser maior que zero.");

        if (pageSize < 1)
            return new BaseResponse<PaginatedResult<AwaitingPartServiceOrder>>(null, 400, "O tamanho da página deve ser maior que zero.");

        var treeMonthAgo = DateTime.UtcNow.AddMonths(-3);

        var query = _context.ServiceOrders
            .AsNoTracking()
            .Where(x => x.ServiceOrderStatus == EServiceOrderStatus.OrderPart &&
                        x.RepairStatus == ERepairStatus.Approved &&
                        x.PurchasePartDate.HasValue &&
                        x.PurchasePartDate >= treeMonthAgo)
            .OrderByDescending(x => x.Id)
            .Select(so => new AwaitingPartServiceOrder(so.Id, so.Customer.Name.CustomerName, so.Product.Model, so.Solution != null ? so.Solution.ServiceOrderSolution : null, so.PurchasePartDate));

        var totalCount = await query.CountAsync();

        var awaitingPartServiceOrders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

        var result = new PaginatedResult<AwaitingPartServiceOrder>(awaitingPartServiceOrders, totalCount, pageNumber, pageSize, totalPages);
        return new BaseResponse<PaginatedResult<AwaitingPartServiceOrder>>(result, 200, "Ordens de serviço aguardando peça recuperadas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ReadyForPickupServiceOrder>>> GetReadyForPickupAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            return new BaseResponse<PaginatedResult<ReadyForPickupServiceOrder>>(null, 400, "O número da página deve ser maior que zero.");

        if (pageSize < 1)
            return new BaseResponse<PaginatedResult<ReadyForPickupServiceOrder>>(null, 400, "O tamanho da página deve ser maior que zero.");

        var sixMonths = DateTime.UtcNow.AddMonths(-6);

        var query = _context.ServiceOrders
            .AsNoTracking()
            .Where(x => 
                (x.RepairDate >= sixMonths || x.RepairDate == null) && 
                (x.ServiceOrderStatus == EServiceOrderStatus.Repaired || x.ServiceOrderStatus == EServiceOrderStatus.ReadyToPickUp))
            .OrderByDescending(x => x.Id)
            .Select(so => new ReadyForPickupServiceOrder(so.Id, so.Customer.Name.CustomerName, so.Product.Model, so.RepairDate, so.RepairResult.ToString(), so.RepairStatus.ToString()));

        var totalCount = await query.CountAsync();

        var readyForPickupServiceOrders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        int? totalPages = totalCount > 0 ? (int?)Math.Ceiling(totalCount / (double)pageSize) : null;

        var result = new PaginatedResult<ReadyForPickupServiceOrder>(readyForPickupServiceOrders, totalCount, pageNumber, pageSize, totalPages);
        return new BaseResponse<PaginatedResult<ReadyForPickupServiceOrder>>(result, 200, "Ordens de serviço aguardando coleta recuperadas com sucesso!");
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