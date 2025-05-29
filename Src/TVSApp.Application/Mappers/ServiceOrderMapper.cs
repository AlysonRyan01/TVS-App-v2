using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Domain.Entities;
using TVS_App.Domain.ValueObjects.Customer;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVS_App.Application.Mappers;

public static class ServiceOrderMapper
{
    public static UpdateServiceOrderResponseDto MapToUpdateResponseDto(ServiceOrder serviceOrder)
    {
        return new UpdateServiceOrderResponseDto
        {
            Id = serviceOrder.Id,
            RepairStatus = serviceOrder.RepairStatus,
            Status = serviceOrder.ServiceOrderStatus,
            TotalAmount = serviceOrder.TotalAmount
        };
    }
    
    public static ServiceOrderDto MapToServiceOrderDto(ServiceOrder serviceOrder)
    {
        return new ServiceOrderDto
        {
            Id = serviceOrder.Id,
            CustomerId = serviceOrder.CustomerId,
            Name = serviceOrder.Customer.Name.CustomerName,
            Street = serviceOrder.Customer.Address.Street,
            Neighborhood = serviceOrder.Customer.Address.Neighborhood,
            City = serviceOrder.Customer.Address.City,
            Number = serviceOrder.Customer.Address.Number,
            ZipCode = serviceOrder.Customer.Address.ZipCode,
            State = serviceOrder.Customer.Address.State,
            Phone = serviceOrder.Customer.Phone.CustomerPhone,
            Phone2 = serviceOrder.Customer.Phone2.CustomerPhone,
            Email = string.IsNullOrWhiteSpace(serviceOrder.Customer.Email?.CustomerEmail) ? null : serviceOrder.Customer.Email.CustomerEmail,
            Cpf = serviceOrder.Customer.Cpf.Number,
            Brand = serviceOrder.Product.Brand,
            Model = serviceOrder.Product.Model,
            SerialNumber = serviceOrder.Product.SerialNumber,
            Defect = serviceOrder.Product.Defect,
            Accessories = serviceOrder.Product.Accessories,
            Type = serviceOrder.Product.Type,
            Location = serviceOrder.Product.Location,
            Enterprise = serviceOrder.Enterprise,
            EntryDate = serviceOrder.EntryDate,
            InspectionDate = serviceOrder.InspectionDate,
            ResponseDate = serviceOrder.ResponseDate,
            RepairDate = serviceOrder.RepairDate,
            PurchasePartDate = serviceOrder.PurchasePartDate,
            DeliveryDate = serviceOrder.DeliveryDate,
            Solution = string.IsNullOrWhiteSpace(serviceOrder.Solution?.ServiceOrderSolution) ? null : serviceOrder.Solution.ServiceOrderSolution,
            EstimateMessage = serviceOrder.EstimateMessage,
            Guarantee = string.IsNullOrWhiteSpace(serviceOrder.Guarantee?.ServiceOrderGuarantee) ? null : serviceOrder.Guarantee.ServiceOrderGuarantee,
            PartCost = serviceOrder.PartCost.ServiceOrderPartCost,
            LaborCost = serviceOrder.LaborCost.ServiceOrderLaborCost,
            ServiceOrderStatus = serviceOrder.ServiceOrderStatus,
            RepairStatus = serviceOrder.RepairStatus,
            RepairResult = serviceOrder.RepairResult
        };
    }
    
    public static ServiceOrder MapToServiceOrder(ServiceOrderDto dto)
    {
        // 1. Criar Customer e Value Objects
        var customer = new Customer(
            new Name(dto.Name),
            new Address(dto.Street, dto.Neighborhood, dto.City, dto.Number, dto.ZipCode, dto.State),
            new Phone(dto.Phone),
            new Phone(dto.Phone2),
            string.IsNullOrWhiteSpace(dto.Email) ? null : new Email(dto.Email),
            new Cpf(dto.Cpf)
        );
        
        var product = new Product(
            dto.Brand,
            dto.Model,
            dto.SerialNumber,
            dto.Defect,
            dto.Accessories,
            dto.Type
        );
        
        var serviceOrder = new ServiceOrder(dto.Enterprise, dto.CustomerId, product);
        
        serviceOrder.UpdateCustomer(customer);
        

        return serviceOrder;
    }
}