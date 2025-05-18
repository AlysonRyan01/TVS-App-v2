using TVS_App.Application.DTOs;
using TVS_App.Application.Repositories;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVS_App.Tests.Mocks;

public class FakeServiceOrderRepository : IServiceOrderRepository
{
    private readonly List<ServiceOrder?> _serviceOrders = new();
    private long _idCounter = 0;

    public FakeServiceOrderRepository()
    {
        CreateAsync(new ServiceOrder(EEnterprise.Particular, 1,
            new Product("PHILIPS", "40PFG5000", "2316765", "Sem imagem", "Cabo e controle", EProduct.Tv)));

        CreateAsync(new ServiceOrder(EEnterprise.Particular, 2,
            new Product("LG", "40LB5600", "3214215", "Tela lisa", "Cabo e controle", EProduct.Tv)));

        CreateAsync(new ServiceOrder(EEnterprise.Particular, 3,
            new Product("SAMSUNG", "UN40J5200AG", "31256457", "Sem som", "Cabo e controle", EProduct.Tv)));

        CreateAsync(new ServiceOrder(EEnterprise.Cocel, 4,
            new Product("LG", "LG1234", "6549876", "Imagem borrada", "Cabo, controle e base", EProduct.Tv)));

        CreateAsync(new ServiceOrder(EEnterprise.Copel, 5,
            new Product("SAMSUNG", "SAMSUNG55XUHD", "12346789", "Desligando sozinho", "Controle remoto", EProduct.Tv)));

        CreateAsync(new ServiceOrder(EEnterprise.Seguradora, 6,
            new Product("SAMSUNG", "Philips6000", "87654321", "Sem sinal", "Cabo HDMI", EProduct.Tv)));

        CreateAsync(new ServiceOrder(EEnterprise.Particular, 7,
            new Product("SAMSUNG", "SonyX9000F", "56789012", "Cor distorcida", "Controle remoto e base", EProduct.Tv)));

        CreateAsync(new ServiceOrder(EEnterprise.Particular, 8,
            new Product("SAMSUNG", "TCL40UHD", "09876543", "Tela preta", "Cabo de força", EProduct.Tv)));

        CreateAsync(new ServiceOrder(EEnterprise.Particular, 9,
            new Product("SAMSUNG", "SamsungUA75", "34567890", "Sem imagem e som", "Cabo HDMI, controle", EProduct.Tv)));

        CreateAsync(new ServiceOrder(EEnterprise.Particular, 10,
            new Product("SAMSUNG", "SamsungUA75", "34567890", "Sem imagem e som", "Cabo HDMI, controle", EProduct.Tv)));

        CreateAsync(new ServiceOrder(EEnterprise.Particular, 11,
            new Product("SAMSUNG", "SamsungUA75", "34567890", "Sem imagem e som", "Cabo HDMI, controle", EProduct.Tv)));

        CreateAsync(new ServiceOrder(EEnterprise.Particular, 12,
            new Product("SAMSUNG", "SamsungUA75", "34567890", "Sem imagem e som", "Cabo HDMI, controle", EProduct.Tv)));

        CreateAsync(new ServiceOrder(EEnterprise.Particular, 13,
            new Product("SAMSUNG", "SamsungUA75", "34567890", "Sem imagem e som", "Cabo HDMI, controle", EProduct.Tv)));

    }

    public Task<BaseResponse<ServiceOrder?>> CreateAsync(ServiceOrder serviceOrder)
    {
        _idCounter++;
        serviceOrder.Id = _idCounter;
        _serviceOrders.Add(serviceOrder);

        return Task.FromResult(new BaseResponse<ServiceOrder?>(serviceOrder, 200, "Ordem de servico criada com sucesso!"));
    }

    public Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetAllAsync(int pageNumber, int pageSize)
    {
        var totalCount = _serviceOrders.Count();

        var paged = _serviceOrders.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        var result = new PaginatedResult<ServiceOrder?>(paged, totalCount, pageNumber, pageSize);
        if (result == null)
            return Task.FromResult(new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 500, "Ocorreu um erro ao obter as ordens de serviço"));

        return Task.FromResult(new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Todos as ordens de servico foram retornadas com sucesso!"));
    }

    public Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByCustomerName(string customerName)
    {
        throw new NotImplementedException();
    }

    public Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersBySerialNumber(string serialNumber)
    {
        throw new NotImplementedException();
    }

    public Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByModel(string model)
    {
        throw new NotImplementedException();
    }

    public Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByEnterprise(EEnterprise enterprise)
    {
        throw new NotImplementedException();
    }

    public Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByDate(DateTime startDate, DateTime endDate)
    {
        throw new NotImplementedException();
    }

    public Task<BaseResponse<ServiceOrder?>> GetById(long id)
    {
        var serviceOrder = _serviceOrders.FirstOrDefault(x => x?.Id == id);
        if (serviceOrder == null)
            return Task.FromResult(new BaseResponse<ServiceOrder?>(null, 404, $"A ordem de serviço com id:{id} nao existe"));

        return Task.FromResult(new BaseResponse<ServiceOrder?>(serviceOrder, 200, $"A ordem de serviço com id:{id} foi obtido com sucesso!"));
    }

    public Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetDeliveredAsync(int pageNumber, int pageSize)
    {
        var query = _serviceOrders.Where(x => x?.ServiceOrderStatus == EServiceOrderStatus.Delivered);
        if (query == null || !query.Any())
            return Task.FromResult(new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 404, "Nenhuma ordem de serviço entregue foi localizada"));

        var totalCount = query.Count();

        var items = query
            .OrderByDescending(x => x?.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PaginatedResult<ServiceOrder?>(items, totalCount, pageNumber, pageSize);

        return Task.FromResult(new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço entregues obtidas com sucesso!"));
    }

    public Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetPendingEstimatesAsync(int pageNumber, int pageSize)
    {
        var query = _serviceOrders.Where(x => x?.ServiceOrderStatus == EServiceOrderStatus.Entered && x.RepairStatus == ERepairStatus.Entered);
        if (query == null || !query.Any())
            return Task.FromResult(new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 404, "Nenhuma ordem de serviço com orçamento pendente foi localizada"));

        var totalCount = query.Count();

        var items = query
            .OrderByDescending(x => x?.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PaginatedResult<ServiceOrder?>(items, totalCount, pageNumber, pageSize);

        return Task.FromResult(new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço com orçamentos pendentes obtidas com sucesso!"));
    }

    public Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetPendingPartPurchase(int pageNumber, int pageSize)
    {
        throw new NotImplementedException();
    }

    public Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetWaitingPartsAsync(int pageNumber, int pageSize)
    {
        var query = _serviceOrders.Where(x => x?.ServiceOrderStatus == EServiceOrderStatus.Evaluated && x.RepairStatus == ERepairStatus.Approved);
        if (query == null || !query.Any())
            return Task.FromResult(new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 404, "Nenhuma ordem de serviço aguardando peça foi localizada"));

        var totalCount = query.Count();

        var items = query
            .OrderByDescending(x => x?.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PaginatedResult<ServiceOrder?>(items, totalCount, pageNumber, pageSize);

        return Task.FromResult(new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço aguardando peça obtidas com sucesso!"));
    }

    public Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetWaitingPickupAsync(int pageNumber, int pageSize)
    {
        var query = _serviceOrders.Where(x => x?.ServiceOrderStatus == EServiceOrderStatus.Repaired ||
            (x?.ServiceOrderStatus == EServiceOrderStatus.Evaluated &&
            (x.RepairStatus == ERepairStatus.Disapproved ||
            x.RepairResult == ERepairResult.Unrepaired ||
            x.RepairResult == ERepairResult.NoDefectFound)));
        if (query == null || !query.Any())
            return Task.FromResult(new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 404, "Nenhuma ordem de serviço aguardando coleta foi localizada"));

        var totalCount = query.Count();

        var items = query
            .OrderByDescending(x => x?.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PaginatedResult<ServiceOrder?>(items, totalCount, pageNumber, pageSize);

        return Task.FromResult(new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço aguardando coleta obtidas com sucesso!"));
    }

    public Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetWaitingResponseAsync(int pageNumber, int pageSize)
    {
        var query = _serviceOrders.Where(x => x?.ServiceOrderStatus == EServiceOrderStatus.Evaluated && x.RepairStatus == ERepairStatus.Waiting);
        if (query == null || !query.Any())
            return Task.FromResult(new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 404, "Nenhuma ordem de serviço aguardando resposta foi localizada"));

        var totalCount = query.Count();

        var items = query
            .OrderByDescending(x => x?.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PaginatedResult<ServiceOrder?>(items, totalCount, pageNumber, pageSize);

        return Task.FromResult(new BaseResponse<PaginatedResult<ServiceOrder?>>(result, 200, "Ordens de serviço aguardando resposta obtidas com sucesso!"));
    }

    public Task<BaseResponse<ServiceOrder?>> UpdateAsync(ServiceOrder serviceOrder)
    {
        var index = _serviceOrders.FindIndex(x => x?.Id == serviceOrder.Id);
        if (index == -1)
            return Task.FromResult(new BaseResponse<ServiceOrder?>(null, 404, "Nenhuma ordem de serviço encontrada"));

        _serviceOrders[index] = serviceOrder;

        return Task.FromResult(new BaseResponse<ServiceOrder?>(_serviceOrders[index], 200, "Ordem de serviço atualizada com sucesso!"));
    }

    public Task TestAddEstimate()
    {
        var serviceOrders = _serviceOrders.Where(x => x != null && x.Id >= 2 && x.Id <= 4).ToList();

        foreach (var serviceOrder in serviceOrders)
            serviceOrder?.AddEstimate("placa", "3 meses", 200m, 300m, ERepairResult.Repair, "");

        return Task.CompletedTask;
    }
}