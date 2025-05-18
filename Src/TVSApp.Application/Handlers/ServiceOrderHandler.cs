using TVS_App.Application.Commands;
using TVS_App.Application.Commands.ServiceOrderCommands;
using TVS_App.Application.DTOs;
using TVS_App.Application.Exceptions;
using TVS_App.Application.Interfaces;
using TVS_App.Application.Repositories;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVS_App.Application.Handlers;

public class ServiceOrderHandler
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IGenerateServiceOrderPdf _generateServiceOrderPdf;


    public ServiceOrderHandler(IServiceOrderRepository serviceOrderRepository, ICustomerRepository customerRepository,
    IGenerateServiceOrderPdf generateServiceOrderPdf)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _customerRepository = customerRepository;
        _generateServiceOrderPdf = generateServiceOrderPdf;
    }

    public async Task<BaseResponse<byte[]>> CreateServiceOrderAndReturnPdfAsync(CreateServiceOrderCommand command)
    {
        try
        {
            command.Normalize();
            command.Validate();

            var product = new Product(command.ProductBrand, command.ProductModel,
                    command.ProductSerialNumber, command.ProductDefect, command.Accessories, command.ProductType);

            var serviceOrder = new ServiceOrder(command.Enterprise, command.CustomerId, product);

            var createServiceOrder = await _serviceOrderRepository.CreateAsync(serviceOrder);
            if (!createServiceOrder.IsSuccess)
                return new BaseResponse<byte[]>(null, 500, createServiceOrder.Message);

            if (createServiceOrder.Data != null)
                serviceOrder = createServiceOrder.Data;

            var createPdf = await _generateServiceOrderPdf.GenerateCheckInDocumentAsync(serviceOrder);
            if (!createPdf.IsSuccess)
                return new BaseResponse<byte[]>(null, 500, createPdf.Message);

            return createPdf;
        }
        catch (CommandException<CreateServiceOrderCommand> ex)
        {
            return new BaseResponse<byte[]>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<byte[]>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<ServiceOrder?>> UpdateServiceOrderAsync(UpdateServiceOrderCommand command)
    {
        try
        {
            command.Normalize();
            command.Validate();

            var existingCustomer = await _customerRepository.GetByIdAsync(command.CustomerId);
            if (existingCustomer.Data == null)
                return new BaseResponse<ServiceOrder?>(null, 404, $"O cliente com id: {command.CustomerId} não existe");

            var customer = existingCustomer.Data;

            var existingServiceOrder = await _serviceOrderRepository.GetById(command.ServiceOrderId);
            if (existingServiceOrder.Data == null)
                return new BaseResponse<ServiceOrder?>(null, 404, $"A ordem de serviço com id: {command.ServiceOrderId} não existe");

            var serviceOrder = existingServiceOrder.Data;

            serviceOrder.UpdateServiceOrder(customer,
                command.ProductBrand,
                command.ProductModel,
                command.ProductSerialNumber,
                command.ProductDefect,
                command.Accessories,
                command.ProductType,
                command.Enterprise);

            return await _serviceOrderRepository.UpdateAsync(serviceOrder);
        }
        catch (CommandException<UpdateServiceOrderCommand> ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }
    
    public async Task<BaseResponse<ServiceOrder?>> EditServiceOrderAsync(ServiceOrder serviceOrder)
    {
        try
        {
            if (serviceOrder.Id == 0)
                return new BaseResponse<ServiceOrder?>(null, 401, "O ID da ordem de serviço não pode ser 0");
            
            return await _serviceOrderRepository.UpdateAsync(serviceOrder);
        }
        catch (CommandException<UpdateServiceOrderCommand> ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<ServiceOrder?>> AddProductLocation(AddProductLocationCommand command)
    {
        try
        {
            command.Normalize();
            command.Validate();

            var result = await _serviceOrderRepository.GetById(command.ServiceOrderId);
            if (result.Data == null)
                return new BaseResponse<ServiceOrder?>(null, 404, "Essa ordem de serviço não existe");

            var serviceOrder = result.Data;

            serviceOrder.Product.AddLocation(command.Location);

            await _serviceOrderRepository.UpdateAsync(serviceOrder);

            return new BaseResponse<ServiceOrder?>(serviceOrder, 200, "Localização do produto adicionada com sucesso!");
        }
        catch (CommandException<GetServiceOrderByIdCommand> ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByCustomerName(string customerName)
    {
        try
        {
            return await _serviceOrderRepository.GetServiceOrdersByCustomerName(customerName);
        }
        catch (CommandException<GetServiceOrderByIdCommand> ex)
        {
            return new BaseResponse<List<ServiceOrder>>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<List<ServiceOrder>>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<ServiceOrder?>> GetServiceOrderById(GetServiceOrderByIdCommand command)
    {
        try
        {
            command.Validate();

            return await _serviceOrderRepository.GetById(command.Id);
        }
        catch (CommandException<GetServiceOrderByIdCommand> ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }
    
    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersBySerialNumberAsync(GetServiceOrdersBySerialNumberCommand command)
    {
        try
        {
            command.Normalize();
            command.Validate();

            return await _serviceOrderRepository.GetServiceOrdersBySerialNumber(command.SerialNumber);
        }
        catch (CommandException<GetServiceOrderByIdCommand> ex)
        {
            return new BaseResponse<List<ServiceOrder>>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<List<ServiceOrder>>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }
    
    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByModelAsync(GetServiceOrdersByModelCommand command)
    {
        try
        {
            command.Normalize();
            command.Validate();

            return await _serviceOrderRepository.GetServiceOrdersByModel(command.Model);
        }
        catch (CommandException<GetServiceOrderByIdCommand> ex)
        {
            return new BaseResponse<List<ServiceOrder>>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<List<ServiceOrder>>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }
    
    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByEnterpriseAsync(EEnterprise enterprise)
    {
        try
        {
            return await _serviceOrderRepository.GetServiceOrdersByEnterprise(enterprise);
        }
        catch (Exception ex)
        {
            return new BaseResponse<List<ServiceOrder>>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }
    
    public async Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByDateAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            return await _serviceOrderRepository.GetServiceOrdersByDate(startDate, endDate);
        }
        catch (Exception ex)
        {
            return new BaseResponse<List<ServiceOrder>>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<EstimateServiceOrder>> GetServiceOrderForCustomer(GetServiceOrdersForCustomerCommand command)
    {
        try
        {
            command.Normalize();
            command.Validate();

            var orderResult = await _serviceOrderRepository.GetById(command.ServiceOrderId);
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
                serviceOrder.Product.Brand, 
                serviceOrder.Product.Model,
                serviceOrder.Product.Defect ?? "",
                serviceOrder.Guarantee?.ServiceOrderGuarantee,
                serviceOrder.TotalAmount, serviceOrder.EstimateMessage);

            return new BaseResponse<EstimateServiceOrder>(dto, 200, "Ordem de serviço obtida com sucesso!");
        }
        catch (CommandException<GetServiceOrdersForCustomerCommand> ex)
        {
            return new BaseResponse<EstimateServiceOrder>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<EstimateServiceOrder>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetAllServiceOrdersAsync(PaginationCommand command)
    {
        try
        {
            command.Validate();

            return await _serviceOrderRepository.GetAllAsync(command.PageNumber, command.PageSize);
        }
        catch (Exception ex)
        {

            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetPendingEstimatesAsync(PaginationCommand command)
    {
        try
        {
            command.Validate();

            return await _serviceOrderRepository.GetPendingEstimatesAsync(command.PageNumber, command.PageSize);
        }
        catch (Exception ex)
        {

            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetWaitingResponseAsync(PaginationCommand command)
    {
        try
        {
            command.Validate();

            return await _serviceOrderRepository.GetWaitingResponseAsync(command.PageNumber, command.PageSize);
        }
        catch (Exception ex)
        {

            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetPendingPurchasePartAsync(PaginationCommand command)
    {
        try
        {
            command.Validate();

            return await _serviceOrderRepository.GetPendingPartPurchase(command.PageNumber, command.PageSize);
        }
        catch (Exception ex)
        {

            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetWaitingPartsAsync(PaginationCommand command)
    {
        try
        {
            command.Validate();

            return await _serviceOrderRepository.GetWaitingPartsAsync(command.PageNumber, command.PageSize);
        }
        catch (Exception ex)
        {

            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetWaitingPickupAsync(PaginationCommand command)
    {
        try
        {
            command.Validate();
        
            return await _serviceOrderRepository.GetWaitingPickupAsync(command.PageNumber, command.PageSize);
        }
        catch (Exception ex)
        {

            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetDeliveredAsync(PaginationCommand command)
    {
        try
        {
            command.Validate();

            return await _serviceOrderRepository.GetDeliveredAsync(command.PageNumber, command.PageSize);
        }
        catch (Exception ex)
        {

            return new BaseResponse<PaginatedResult<ServiceOrder?>>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<ServiceOrder?>> AddServiceOrderEstimate(AddServiceOrderEstimateCommand command)
    {
        try
        {
            command.Normalize();
            command.Validate();

            var result = await _serviceOrderRepository.GetById(command.ServiceOrderId);
            if (result.Data == null)
                return new BaseResponse<ServiceOrder?>(null, 404, "Essa ordem de serviço não existe");

            var serviceOrder = result.Data;

            serviceOrder.AddEstimate(command.Solution, command.Guarantee, command.PartCost, command.LaborCost, command.RepairResult, command.EstimateMessage);

            await _serviceOrderRepository.UpdateAsync(serviceOrder);

            return new BaseResponse<ServiceOrder?>(serviceOrder, 200, "Orçamento adicionado com sucesso!");
        }
        catch (CommandException<AddServiceOrderEstimateCommand> ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<ServiceOrder?>> AddServiceOrderApproveEstimate(GetServiceOrderByIdCommand command)
    {
        try
        {
            command.Validate();

            var result = await _serviceOrderRepository.GetById(command.Id);
            if (result.Data == null)
                return new BaseResponse<ServiceOrder?>(null, 404, "Essa ordem de serviço não existe");

            var serviceOrder = result.Data;

            serviceOrder.ApproveEstimate();

            await _serviceOrderRepository.UpdateAsync(serviceOrder);

            return new BaseResponse<ServiceOrder?>(serviceOrder, 200, "Orçamento aprovado com sucesso!");
        }
        catch (CommandException<GetServiceOrderByIdCommand> ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<ServiceOrder?>> AddServiceOrderRejectEstimate(GetServiceOrderByIdCommand command)
    {
        try
        {
            command.Validate();

            var result = await _serviceOrderRepository.GetById(command.Id);
            if (result.Data == null)
                return new BaseResponse<ServiceOrder?>(null, 404, "Essa ordem de serviço não existe");

            var serviceOrder = result.Data;

            serviceOrder.RejectEstimate();

            await _serviceOrderRepository.UpdateAsync(serviceOrder);

            return new BaseResponse<ServiceOrder?>(serviceOrder, 200, "Orçamento reprovado com sucesso!");
        }
        catch (CommandException<GetServiceOrderByIdCommand> ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<ServiceOrder?>> AddPurchasedPart(GetServiceOrderByIdCommand command)
    {
        try
        {
            command.Validate();

            var result = await _serviceOrderRepository.GetById(command.Id);
            if (result.Data == null)
                return new BaseResponse<ServiceOrder?>(null, 404, "Essa ordem de serviço não existe");

            var serviceOrder = result.Data;

            serviceOrder.AddPurchasedPart();

            await _serviceOrderRepository.UpdateAsync(serviceOrder);

            return new BaseResponse<ServiceOrder?>(serviceOrder, 200, "Compra de peça adicionada com sucesso!");
        }
        catch (CommandException<GetServiceOrderByIdCommand> ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<ServiceOrder?>> AddServiceOrderRepair(GetServiceOrderByIdCommand command)
    {
        try
        {
            command.Validate();

            var result = await _serviceOrderRepository.GetById(command.Id);
            if (result.Data == null)
                return new BaseResponse<ServiceOrder?>(null, 404, "Essa ordem de serviço não existe");

            var serviceOrder = result.Data;

            serviceOrder.ExecuteRepair();

            await _serviceOrderRepository.UpdateAsync(serviceOrder);

            return new BaseResponse<ServiceOrder?>(serviceOrder, 200, "Ordem de serviço marcada como consertada!");
        }
        catch (CommandException<GetServiceOrderByIdCommand> ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<ServiceOrder?>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<byte[]>> AddServiceOrderDeliveryAndReturnPdfAsync(GetServiceOrderByIdCommand command)
    {
        try
        {
            command.Validate();

            var result = await _serviceOrderRepository.GetById(command.Id);
            if (result.Data == null)
                return new BaseResponse<byte[]>(null, 404, "Essa ordem de serviço não existe");

            var serviceOrder = result.Data;

            serviceOrder.AddDelivery();

            await _serviceOrderRepository.UpdateAsync(serviceOrder);

            if (serviceOrder.RepairStatus == ERepairStatus.Approved &&
                serviceOrder.RepairResult == ERepairResult.Repair)
            {
                var createPdf = await _generateServiceOrderPdf.GenerateCheckOutDocumentAsync(serviceOrder);
                if (!createPdf.IsSuccess)
                    return new BaseResponse<byte[]>(null, 500, createPdf.Message);

                return createPdf;
            }

            return new BaseResponse<byte[]>(null, 200, "Ordem de serviço entregue com sucesso!");
        }
        catch (CommandException<GetServiceOrderByIdCommand> ex)
        {
            return new BaseResponse<byte[]>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<byte[]>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }
    
    public async Task<BaseResponse<byte[]>> RegenerateAndReturnPdfAsync(GetServiceOrderByIdCommand command)
    {
        try
        {
            command.Validate();

            var result = await _serviceOrderRepository.GetById(command.Id);
            if (result.Data == null)
                return new BaseResponse<byte[]>(null, 404, "Essa ordem de serviço não existe");

            var serviceOrder = result.Data;

            var createPdf = await _generateServiceOrderPdf.RegeneratePdfAsync(serviceOrder);
            if (!createPdf.IsSuccess)
                return new BaseResponse<byte[]>(null, 500, createPdf.Message);

            return createPdf;
        }
        catch (CommandException<GetServiceOrderByIdCommand> ex)
        {
            return new BaseResponse<byte[]>(null, 400, $"Erro de validação: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<byte[]>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }
}