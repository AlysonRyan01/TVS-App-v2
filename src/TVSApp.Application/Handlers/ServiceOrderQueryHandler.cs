using TVS_App.Application.DTOs;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;
using TVS_App.Domain.Repositories;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers;

public class ServiceOrderQueryHandler : IServiceOrderQueryHandler
{
    private readonly IServiceOrderQueryRepository _repository;

    public ServiceOrderQueryHandler(IServiceOrderQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaseResponse<ServiceOrderDto>> GetServiceOrderById(long id)
    {
        if (id <= 0)
            return new BaseResponse<ServiceOrderDto>(null, 400, "ID da ordem de serviço inválido");
        
        var repositoryResult = await _repository.GetByIdAsync(id);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<ServiceOrderDto>(null, 500, repositoryResult.Message);
        
        var dto =  repositoryResult.Data.MapToServiceOrderDto();
        return new BaseResponse<ServiceOrderDto>(dto);
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrderSearchItem>>> Search(ServiceOrderSearchQuery query)
    {
        var customer = Clean(query.Customer);
        var model = Clean(query.Model);
        var serialNumber = Clean(query.SerialNumber);
        var hasPeriod = query.From.HasValue || query.To.HasValue;

        var filled = new[] { query.Number.HasValue, customer != null, model != null, serialNumber != null, hasPeriod }
            .Count(x => x);
        if (filled > 1)
            return BadSearch("Preencha um filtro por vez.");

        if (query.Number is <= 0)
            return BadSearch("Informe um número de OS válido.");

        if (customer is { Length: < 3 })
            return BadSearch("Digite pelo menos 3 letras do cliente.");

        var from = query.From?.Date;
        var to = query.To?.Date ?? (from.HasValue ? DateTime.Today : null);
        if (from.HasValue && to.HasValue && to < from)
            return BadSearch("A data final não pode ser antes da inicial.");

        if (query.PageNumber < 1 || query.PageSize is < 1 or > 100)
            return BadSearch("Paginação inválida.");

        var filter = new ServiceOrderSearchFilter(query.Number, customer, model, serialNumber, from, to);
        var result = await _repository.SearchAsync(filter, query.PageNumber, query.PageSize);
        if (!result.IsSuccess || result.Data == null)
            return new BaseResponse<PaginatedResult<ServiceOrderSearchItem>>(null, result.Code, result.Message);

        var message = result.Data.TotalCount == 0
            ? "Nenhuma ordem de serviço encontrada."
            : $"{result.Data.TotalCount} ordens encontradas.";
        return new BaseResponse<PaginatedResult<ServiceOrderSearchItem>>(result.Data, 200, message);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BaseResponse<PaginatedResult<ServiceOrderSearchItem>> BadSearch(string message)
        => new(null, 400, message);

    public async Task<BaseResponse<EstimateServiceOrder>> GetForCustomer(long id, string code)
    {
        var result = await _repository.GetByIdAsync(id);
        var serviceOrder = result.Data;
        if (serviceOrder == null || !SecurityCodeMatches(serviceOrder.SecurityCode, code))
            return new BaseResponse<EstimateServiceOrder>(null, 404, "Essa ordem de serviço não existe");

        var dto = new EstimateServiceOrder(
            serviceOrder.Id,
            serviceOrder.Customer.Name.CustomerName,
            serviceOrder.Product.Type,
            serviceOrder.Product.Brand,
            serviceOrder.Product.Model,
            serviceOrder.Product.Defect ?? "",
            serviceOrder.Guarantee?.ServiceOrderGuarantee,
            serviceOrder.TotalAmount,
            serviceOrder.EstimateMessage,
            serviceOrder.ServiceOrderStatus,
            serviceOrder.RepairResult,
            serviceOrder.RepairStatus,
            serviceOrder.Enterprise);

        return new BaseResponse<EstimateServiceOrder>(dto, 200, "Ordem de serviço obtida com sucesso!");
    }

    public async Task<BaseResponse<List<PaymentDto>>> GetPayments(long serviceOrderId)
    {
        var result = await _repository.GetByIdAsync(serviceOrderId);
        if (result.Data == null)
            return new BaseResponse<List<PaymentDto>>(null, 404, "Essa ordem de serviço não existe");

        var payments = result.Data.Payments
            .Select(p => new PaymentDto(p.Id, p.Amount, p.PaidAt))
            .ToList();

        return new BaseResponse<List<PaymentDto>>(payments);
    }

    public static bool SecurityCodeMatches(string securityCode, string? informed)
        => !string.IsNullOrWhiteSpace(informed)
           && string.Equals(securityCode, informed.Trim().ToUpper(), StringComparison.Ordinal);

    public Task<BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>> GetPendingEstimateServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetPendingEstimateAsync);

    public Task<BaseResponse<PaginatedResult<AwaitingResponseServiceOrder>>> GetAwaitingResponseServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetAwaitingResponseAsync);

    public Task<BaseResponse<PaginatedResult<ApprovedServiceOrder>>> GetApprovedServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetApprovedAsync);

    public Task<BaseResponse<PaginatedResult<RejectedServiceOrder>>> GetRejectedServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetRejectedAsync);

    public Task<BaseResponse<PaginatedResult<AwaitingPartServiceOrder>>> GetAwaitingPartServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetAwaitingPartAsync);

    public Task<BaseResponse<PaginatedResult<ReadyForPickupServiceOrder>>> GetReadyForPickupServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetReadyForPickupAsync);

    private static async Task<BaseResponse<PaginatedResult<T>>> GetPagedAsync<T>(
        int pageNumber,
        int pageSize,
        Func<int, int, Task<BaseResponse<PaginatedResult<T>>>> query)
    {
        if (!ValidatePagination(pageNumber, pageSize))
            return new BaseResponse<PaginatedResult<T>>(null, 400, "Paginação inválida");

        var repositoryResult = await query(pageNumber, pageSize);
        if (!repositoryResult.IsSuccess)
            return new BaseResponse<PaginatedResult<T>>(null, 500, repositoryResult.Message);

        return repositoryResult;
    }

    private static bool ValidatePagination(int pageNumber, int pageSize)
        => pageNumber >= 1 && pageSize >= 1;
}