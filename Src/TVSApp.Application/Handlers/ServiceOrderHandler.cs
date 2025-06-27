using TVS_App.Application.Commands;
using TVS_App.Application.Commands.ServiceOrderCommands;
using TVS_App.Application.DTOs;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Interfaces;
using TVS_App.Application.Mappers;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVS_App.Application.Handlers;

public class ServiceOrderHandler
{
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;
    private readonly ICustomerQueryRepository _customerQueryRepository;
    private readonly IGenerateServiceOrderPdf _generateServiceOrderPdf;
    private readonly IWhatsappService _whatsappService;
    
    public ServiceOrderHandler(
        IServiceOrderCommandRepository serviceOrderCommandRepository,
        IServiceOrderQueryRepository serviceOrderQueryRepository,
        ICustomerQueryRepository customerQueryRepository,
        IGenerateServiceOrderPdf generateServiceOrderPdf,
        IWhatsappService whatsappService)
    {
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
        _customerQueryRepository = customerQueryRepository;
        _generateServiceOrderPdf = generateServiceOrderPdf;
        _whatsappService = whatsappService;
    }

    public async Task<BaseResponse<byte[]>> CreateServiceOrderAndReturnPdfAsync(CreateServiceOrderCommand command)
    {
        command.Normalize();
        command.Validate();

        var product = new Product(command.ProductBrand, command.ProductModel,
            command.ProductSerialNumber, command.ProductDefect, command.Accessories, command.ProductType);

        var serviceOrder = new ServiceOrder(command.Enterprise, command.CustomerId, product);

        var createServiceOrder = await _serviceOrderCommandRepository.CreateAsync(serviceOrder);
        if (!createServiceOrder.IsSuccess)
            return new BaseResponse<byte[]>(null, 500, createServiceOrder.Message);

        if (createServiceOrder.Data != null)
            serviceOrder = createServiceOrder.Data;

        var createPdf = await _generateServiceOrderPdf.GenerateCheckInDocumentAsync(serviceOrder);
        if (!createPdf.IsSuccess)
            return new BaseResponse<byte[]>(null, 500, createPdf.Message);
            
        await _whatsappService.SendWelcomeMessage(
            serviceOrder.Id.ToString(), 
            serviceOrder.Customer.Name.CustomerName, 
            serviceOrder.Customer.Phone.CustomerPhone);

        return createPdf;
    }
    
    public async Task<BaseResponse<byte[]>> CreateSalesServiceOrderAsync(CreateSalesServiceOrderCommand command)
    {
        command.Normalize();
        command.Validate();

        var product = new Product(command.ProductBrand, command.ProductModel,
            command.ProductSerialNumber, "VENDA", "", command.ProductType);

        var serviceOrder = new ServiceOrder(EEnterprise.Particular, command.CustomerId, product);
            
        serviceOrder.EditGuarantee(new Guarantee(command.Guarantee));
        serviceOrder.EditPartCost(new PartCost(command.Amount));
        serviceOrder.EditServiceOrderStatus(EServiceOrderStatus.Delivered);
        serviceOrder.EditRepairResult(ERepairResult.Repair);
        serviceOrder.EditRepairStatus(ERepairStatus.Approved);
        serviceOrder.EditDeliveryDate(DateTime.UtcNow);

        var createServiceOrder = await _serviceOrderCommandRepository.CreateAsync(serviceOrder);
        if (!createServiceOrder.IsSuccess)
            return new BaseResponse<byte[]>(null, 500, createServiceOrder.Message);

        if (createServiceOrder.Data != null)
            serviceOrder = createServiceOrder.Data;

        var createPdf = await _generateServiceOrderPdf.GenerateSaleDocumentAsync(serviceOrder);
        if (!createPdf.IsSuccess)
            return new BaseResponse<byte[]>(null, 500, createPdf.Message);
            
        await _whatsappService.SendProductSaleMessage(
            serviceOrder.Customer.Name.CustomerName, 
            serviceOrder.Customer.Phone.CustomerPhone, 
            serviceOrder.PartCost.ServiceOrderPartCost, 
            serviceOrder.Guarantee!.ServiceOrderGuarantee, 
            serviceOrder.Product.Type);

        return createPdf;
    }

    public async Task<BaseResponse<UpdateServiceOrderResponseDto?>> UpdateServiceOrderAsync(UpdateServiceOrderCommand command)
    {
        command.Normalize();
        command.Validate();

        var existingCustomer = await _customerQueryRepository.GetByIdAsync(command.CustomerId);
        if (existingCustomer.Data == null)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 404, $"O cliente com id: {command.CustomerId} não existe");

        var customer = existingCustomer.Data;

        var existingServiceOrder = await _serviceOrderQueryRepository.GetById(command.ServiceOrderId);
        if (existingServiceOrder.Data == null)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 404, $"A ordem de serviço com id: {command.ServiceOrderId} não existe");

        var serviceOrder = existingServiceOrder.Data;

        serviceOrder.UpdateServiceOrder(customer,
            command.ProductBrand,
            command.ProductModel,
            command.ProductSerialNumber,
            command.ProductDefect,
            command.Accessories,
            command.ProductType,
            command.Enterprise);

        var result = await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);
        if (!result.IsSuccess || result.Data == null)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 500, result.Message);
            
        var dto = ServiceOrderMapper.MapToUpdateResponseDto(result.Data);
            
        return new BaseResponse<UpdateServiceOrderResponseDto?>(dto, 200, result.Message);
    }
    
    public async Task<BaseResponse<UpdateServiceOrderResponseDto?>> EditServiceOrderAsync(EditServiceOrderCommand command)
    {
        command.Normalize();
        command.Validate();
            
        if (command.Id == 0)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 401, "O ID da ordem de serviço não pode ser 0");
            
        var repositoryResult = await _serviceOrderQueryRepository.GetById(command.Id);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 500, repositoryResult.Message);
            
        var serviceOrder = repositoryResult.Data;
            
        ServiceOrderMapper.ApplyChanges(serviceOrder, command);
            
        var result = await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);
        if (!result.IsSuccess || result.Data == null)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 500, result.Message);
            
        var dto = ServiceOrderMapper.MapToUpdateResponseDto(result.Data);
            
        return new BaseResponse<UpdateServiceOrderResponseDto?>(dto, 200, result.Message);
    }

    public async Task<BaseResponse<string?>> AddProductLocation(AddProductLocationCommand command)
    {
        command.Normalize();
        command.Validate();

        var result = await _serviceOrderQueryRepository.GetById(command.ServiceOrderId);
        if (result.Data == null)
            return new BaseResponse<string?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.Product.AddLocation(command.Location);

        await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);

        return new BaseResponse<string?>(serviceOrder.Product.Location, 200, "Localização do produto adicionada com sucesso!");
    }

    public async Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersByCustomerName(string customerName)
    {
        var repositoryResult = await _serviceOrderQueryRepository.GetServiceOrdersByCustomerName(customerName);
        if (!repositoryResult.IsSuccess)
            return new BaseResponse<List<ServiceOrderDto>>(null, 500, repositoryResult.Message);
            
        var serviceOrders = repositoryResult.Data;
            
        var dto = ServiceOrderMapper.MapToServiceOrderDtoList(serviceOrders ?? new List<ServiceOrder>());
            
        return new BaseResponse<List<ServiceOrderDto>>(dto, 200, "Ordens de serviço obtidas com sucesso!");
    }

    public async Task<BaseResponse<ServiceOrderDto?>> GetServiceOrderById(GetServiceOrderByIdCommand command)
    {
        command.Validate();

        var result =  await _serviceOrderQueryRepository.GetById(command.Id);
            
        if (!result.IsSuccess || result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 500, result.Message);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDto(result.Data);
            
        return new BaseResponse<ServiceOrderDto?>(dto, 200, result.Message);
    }
    
    public async Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersBySerialNumberAsync(GetServiceOrdersBySerialNumberCommand command)
    {
        command.Normalize();
        command.Validate();

        var repositoryResult = await _serviceOrderQueryRepository.GetServiceOrdersBySerialNumber(command.SerialNumber);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<List<ServiceOrderDto>>(null, 500, repositoryResult.Message);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDtoList(repositoryResult.Data);
            
        return new BaseResponse<List<ServiceOrderDto>>(dto, 200, "Ordens de serviço obtidas com sucesso!");
    }
    
    public async Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersByModelAsync(GetServiceOrdersByModelCommand command)
    {
        command.Normalize();
        command.Validate();

        var repositoryResult = await _serviceOrderQueryRepository.GetServiceOrdersByModel(command.Model);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<List<ServiceOrderDto>>(null, 500, repositoryResult.Message);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDtoList(repositoryResult.Data);
            
        return new BaseResponse<List<ServiceOrderDto>>(dto, 200, "Ordens de serviço obtidas com sucesso!");
    }
    
    public async Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersByEnterpriseAsync(EEnterprise enterprise)
    {
        var repositoryResult = await _serviceOrderQueryRepository.GetServiceOrdersByEnterprise(enterprise);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<List<ServiceOrderDto>>(null, 500, repositoryResult.Message);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDtoList(repositoryResult.Data);
            
        return new BaseResponse<List<ServiceOrderDto>>(dto, 200, "Ordens de serviço obtidas com sucesso!");
    }
    
    public async Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersByDateAsync(DateTime startDate, DateTime endDate)
    {
        var repositoryResult = await _serviceOrderQueryRepository.GetServiceOrdersByDate(startDate, endDate);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<List<ServiceOrderDto>>(null, 500, repositoryResult.Message);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDtoList(repositoryResult.Data);
            
        return new BaseResponse<List<ServiceOrderDto>>(dto, 200, "Ordens de serviço obtidas com sucesso!");
    }

    public async Task<BaseResponse<EstimateServiceOrder>> GetServiceOrderForCustomer(GetServiceOrdersForCustomerCommand command)
    {
        command.Normalize();
        command.Validate();

        var orderResult = await _serviceOrderQueryRepository.GetById(command.ServiceOrderId);
        if (!orderResult.IsSuccess)
            return new BaseResponse<EstimateServiceOrder>(null, 404, "Nenhuma ordem de serviço encontrada");

        var serviceOrder = orderResult.Data;

        if (!string.Equals(serviceOrder?.SecurityCode, command.SecurityCode))
            return new BaseResponse<EstimateServiceOrder>(null, 404, "O código de segurança está incorreto");
            
        if (serviceOrder == null)
            return new BaseResponse<EstimateServiceOrder>(null, 404, "Ocorreu um erro ao buscar a ordem de serviço");
            
        var dto = new EstimateServiceOrder(
            serviceOrder.Id, 
            serviceOrder.Customer.Name.CustomerName,
            serviceOrder.Product.Type,
            serviceOrder.Product.Brand, 
            serviceOrder.Product.Model,
            serviceOrder.Product.Defect ?? "",
            serviceOrder.Guarantee?.ServiceOrderGuarantee,
            serviceOrder.TotalAmount, serviceOrder.EstimateMessage,
            serviceOrder.ServiceOrderStatus,
            serviceOrder.RepairResult,
            serviceOrder.RepairStatus,
            serviceOrder.Enterprise);

        return new BaseResponse<EstimateServiceOrder>(dto, 200, "Ordem de serviço obtida com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetAllServiceOrdersAsync(PaginationCommand command)
    {
        command.Validate();

        var repositoryResult = await _serviceOrderQueryRepository.GetAllAsync(command.PageNumber, command.PageSize);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, repositoryResult.Message);
            
        var serviceOrders = repositoryResult.Data.Items
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var dtoList = ServiceOrderMapper.MapToServiceOrderDtoList(serviceOrders);

        var paginatedResult = new PaginatedResult<ServiceOrderDto?>(
            dtoList,
            repositoryResult.Data.TotalCount,
            repositoryResult.Data.PageNumber,
            repositoryResult.Data.PageSize
        );

        return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(paginatedResult, 200, "Ordens de serviço obtidas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetPendingEstimatesAsync(PaginationCommand command)
    {
        command.Validate();

        var repositoryResult = await _serviceOrderQueryRepository.GetPendingEstimatesAsync(command.PageNumber, command.PageSize);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, repositoryResult.Message);
            
        var serviceOrders = repositoryResult.Data.Items
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var dtoList = ServiceOrderMapper.MapToServiceOrderDtoList(serviceOrders);

        var paginatedResult = new PaginatedResult<ServiceOrderDto?>(
            dtoList,
            repositoryResult.Data.TotalCount,
            repositoryResult.Data.PageNumber,
            repositoryResult.Data.PageSize
        );

        return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(paginatedResult, 200, "Ordens de serviço obtidas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetWaitingResponseAsync(PaginationCommand command)
    {
        command.Validate();

        var repositoryResult = await _serviceOrderQueryRepository.GetWaitingResponseAsync(command.PageNumber, command.PageSize);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, repositoryResult.Message);
            
        var serviceOrders = repositoryResult.Data.Items
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var dtoList = ServiceOrderMapper.MapToServiceOrderDtoList(serviceOrders);

        var paginatedResult = new PaginatedResult<ServiceOrderDto?>(
            dtoList,
            repositoryResult.Data.TotalCount,
            repositoryResult.Data.PageNumber,
            repositoryResult.Data.PageSize
        );

        return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(paginatedResult, 200, "Ordens de serviço obtidas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetPendingPurchasePartAsync(PaginationCommand command)
    {
        command.Validate();

        var repositoryResult = await _serviceOrderQueryRepository.GetPendingPartPurchase(command.PageNumber, command.PageSize);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, repositoryResult.Message);
            
        var serviceOrders = repositoryResult.Data.Items
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var dtoList = ServiceOrderMapper.MapToServiceOrderDtoList(serviceOrders);

        var paginatedResult = new PaginatedResult<ServiceOrderDto?>(
            dtoList,
            repositoryResult.Data.TotalCount,
            repositoryResult.Data.PageNumber,
            repositoryResult.Data.PageSize
        );

        return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(paginatedResult, 200, "Ordens de serviço obtidas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetWaitingPartsAsync(PaginationCommand command)
    {
        command.Validate();

        var repositoryResult = await _serviceOrderQueryRepository.GetWaitingPartsAsync(command.PageNumber, command.PageSize);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, repositoryResult.Message);
            
        var serviceOrders = repositoryResult.Data.Items
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var dtoList = ServiceOrderMapper.MapToServiceOrderDtoList(serviceOrders);

        var paginatedResult = new PaginatedResult<ServiceOrderDto?>(
            dtoList,
            repositoryResult.Data.TotalCount,
            repositoryResult.Data.PageNumber,
            repositoryResult.Data.PageSize
        );

        return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(paginatedResult, 200, "Ordens de serviço obtidas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetWaitingPickupAsync(PaginationCommand command)
    {
        command.Validate();
        
        var repositoryResult = await _serviceOrderQueryRepository.GetWaitingPickupAsync(command.PageNumber, command.PageSize);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, repositoryResult.Message);
            
        var serviceOrders = repositoryResult.Data.Items
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var dtoList = ServiceOrderMapper.MapToServiceOrderDtoList(serviceOrders);

        var paginatedResult = new PaginatedResult<ServiceOrderDto?>(
            dtoList,
            repositoryResult.Data.TotalCount,
            repositoryResult.Data.PageNumber,
            repositoryResult.Data.PageSize
        );

        return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(paginatedResult, 200, "Ordens de serviço obtidas com sucesso!");
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetDeliveredAsync(PaginationCommand command)
    {
        command.Validate();

        var repositoryResult = await _serviceOrderQueryRepository.GetDeliveredAsync(command.PageNumber, command.PageSize);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, repositoryResult.Message);
            
        var serviceOrders = repositoryResult.Data.Items
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var dtoList = ServiceOrderMapper.MapToServiceOrderDtoList(serviceOrders);

        var paginatedResult = new PaginatedResult<ServiceOrderDto?>(
            dtoList,
            repositoryResult.Data.TotalCount,
            repositoryResult.Data.PageNumber,
            repositoryResult.Data.PageSize
        );

        return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(paginatedResult, 200, "Ordens de serviço obtidas com sucesso!");
    }

    public async Task<BaseResponse<ServiceOrderDto?>> AddServiceOrderEstimate(AddServiceOrderEstimateCommand command)
    {
        command.Normalize();
        command.Validate();

        var result = await _serviceOrderQueryRepository.GetById(command.ServiceOrderId);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.AddEstimate(command.Solution, command.Guarantee, command.PartCost, command.LaborCost, command.RepairResult, command.EstimateMessage);

        await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);

        var message = "Orçamento adicionado com sucesso!";

        if (serviceOrder.Enterprise != EEnterprise.PhilcoBritania)
        {
            var whatsappResult = await _whatsappService.SendEstimateMessage(
                serviceOrder.Id.ToString(),
                serviceOrder.RepairResult!.Value,
                serviceOrder.Customer.Name.CustomerName, 
                command.EstimateMessage.ToUpper(),
                serviceOrder.TotalAmount,
                serviceOrder.Guarantee!.ServiceOrderGuarantee,
                serviceOrder.Customer.Phone.CustomerPhone);
            
            message = whatsappResult.IsSuccess
                ? "Orçamento adicionado e mensagem enviada com sucesso!"
                : $"Orçamento adicionado, mas falha ao enviar mensagem";
        }
            
        var dto = ServiceOrderMapper.MapToServiceOrderDto(serviceOrder);

        return new BaseResponse<ServiceOrderDto?>(dto, 200, message);
    }

    public async Task<BaseResponse<ServiceOrderDto?>> AddServiceOrderApproveEstimate(GetServiceOrderByIdCommand command)
    {
        command.Validate();

        var result = await _serviceOrderQueryRepository.GetById(command.Id);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.ApproveEstimate();

        await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDto(serviceOrder);

        return new BaseResponse<ServiceOrderDto?>(dto, 200, "Aprovação adicionada com sucesso!");
    }

    public async Task<BaseResponse<ServiceOrderDto?>> AddServiceOrderRejectEstimate(GetServiceOrderByIdCommand command)
    {
        command.Validate();

        var result = await _serviceOrderQueryRepository.GetById(command.Id);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.RejectEstimate();

        await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDto(serviceOrder);

        return new BaseResponse<ServiceOrderDto?>(dto, 200, "Orçamento reprovado com sucesso!");
    }

    public async Task<BaseResponse<ServiceOrderDto?>> AddPurchasedPart(GetServiceOrderByIdCommand command)
    {
        command.Validate();

        var result = await _serviceOrderQueryRepository.GetById(command.Id);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.AddPurchasedPart();

        await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDto(serviceOrder);

        return new BaseResponse<ServiceOrderDto?>(dto, 200, "Compra de peça adicionada com sucesso!");
    }

    public async Task<BaseResponse<ServiceOrderDto?>> AddServiceOrderRepair(GetServiceOrderByIdCommand command)
    {
        command.Validate();

        var result = await _serviceOrderQueryRepository.GetById(command.Id);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.ExecuteRepair();

        await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDto(serviceOrder);

        var messageResult = await _whatsappService.SendDeviceReadyMessage(
            serviceOrder.Id.ToString(),
            serviceOrder.Customer.Name.CustomerName,
            serviceOrder.Customer.Phone.CustomerPhone);
            
        var message = messageResult.IsSuccess
            ? "Ordem marcada como consertada e mensagem enviada com sucesso!"
            : $"Ordem marcada como consertada, mas falha ao enviar mensagem";

        return new BaseResponse<ServiceOrderDto?>(dto, 200, message);
    }

    public async Task<BaseResponse<byte[]>> AddServiceOrderDeliveryAndReturnPdfAsync(GetServiceOrderByIdCommand command)
    {
        command.Validate();

        var result = await _serviceOrderQueryRepository.GetById(command.Id);
        if (result.Data == null)
            return new BaseResponse<byte[]>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.AddDelivery();

        await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);

        if (serviceOrder.RepairStatus == ERepairStatus.Approved &&
            serviceOrder.RepairResult == ERepairResult.Repair)
        {
            var createPdf = await _generateServiceOrderPdf.GenerateCheckOutDocumentAsync(serviceOrder);
            if (!createPdf.IsSuccess)
                return new BaseResponse<byte[]>(null, 500, createPdf.Message);

            await _whatsappService.SendGuaranteeMessage(
                serviceOrder.Id.ToString(), 
                serviceOrder.Customer.Name.CustomerName, 
                serviceOrder.Guarantee!.ServiceOrderGuarantee, 
                serviceOrder.Customer.Phone.CustomerPhone);
                
            return createPdf;
        }

        return new BaseResponse<byte[]>(null, 200, "Ordem de serviço entregue com sucesso!");
    }
    
    public async Task<BaseResponse<byte[]>> RegenerateAndReturnPdfAsync(GetServiceOrderByIdCommand command)
    {
        command.Validate();

        var result = await _serviceOrderQueryRepository.GetById(command.Id);
        if (result.Data == null)
            return new BaseResponse<byte[]>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        var createPdf = await _generateServiceOrderPdf.RegeneratePdfAsync(serviceOrder);
        if (!createPdf.IsSuccess)
            return new BaseResponse<byte[]>(null, 500, createPdf.Message);

        return createPdf;
    }
}