using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.DTOs;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.CustomerRequests.CustomerCommands;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;
using TVS_App.Domain.ValueObjects.Customer;

namespace TVS_App.Application.Handlers.Customers;

public class CustomerCommandHandler : ICustomerCommandHandler
{
    private readonly ICustomerCommandRepository _customerCommandRepository;
    private readonly ICustomerQueryRepository _customerQueryRepository;
    private readonly ICustomerSearchCache _customerSearchCache;

    public CustomerCommandHandler(
        ICustomerCommandRepository customerCommandRepository,
        ICustomerQueryRepository customerQueryRepository,
        ICustomerSearchCache customerSearchCache)
    {
        _customerCommandRepository = customerCommandRepository;
        _customerQueryRepository = customerQueryRepository;
        _customerSearchCache = customerSearchCache;
    }

    public async Task<BaseResponse<CustomerDto?>> Create(CreateCustomerCommand command)
    {
        var normalized = command.Normalize();
        normalized.Validate();

        var name = new Name(normalized.Name);
        var address = new Address(
            normalized.Street,
            normalized.Neighborhood,
            normalized.City,
            normalized.Number,
            normalized.ZipCode,
            normalized.State);
        var phone = new Phone(normalized.Phone);
        var phone2 = new Phone(normalized.Phone2);
        Email? email = string.IsNullOrWhiteSpace(normalized.Email) ? null : new Email(normalized.Email);
        var document = Document.FromOptional(normalized.Document);

        var customer = new Customer(name, address, phone, phone2, email, document);

        await _customerCommandRepository.CreateAsync(customer);
        // A busca de clientes (barra "Buscar cliente") vem de um índice em memória; invalida para a próxima busca ver a gravação.
        _customerSearchCache.Invalidate();
            
        var dto = CustomerMapper.MapToDto(customer);
            
        return new BaseResponse<CustomerDto?>(dto, 200, "Cliente cadastrado com sucesso");
    }

    public async Task<BaseResponse<CustomerDto?>> Update(long id, UpdateCustomerCommand command)
    {
        var normalized = (command with { Id = id }).Normalize();
        normalized.Validate();

        var existingCustomer = await _customerQueryRepository.GetByIdAsync(normalized.Id);

        if (!existingCustomer.IsSuccess || existingCustomer.Data is null)
            return new BaseResponse<CustomerDto?>(null, 404, "Cliente não encontrado.");

        var customer = existingCustomer.Data;

        customer.UpdateName(normalized.Name);
        customer.UpdateAdress(
            normalized.Street,
            normalized.Neighborhood,
            normalized.City,
            normalized.Number,
            normalized.ZipCode,
            normalized.State);
        customer.UpdatePhone(normalized.Phone, normalized.Phone2);
        customer.UpdateEmail(normalized.Email);
        customer.UpdateDocument(normalized.Document);

        await _customerCommandRepository.UpdateAsync(customer);
        // A busca de clientes (barra "Buscar cliente") vem de um índice em memória; invalida para a próxima busca ver a gravação.
        _customerSearchCache.Invalidate();
            
        var dto = CustomerMapper.MapToDto(customer);
            
        return new BaseResponse<CustomerDto?>(dto, 200, "Cliente atualizado com sucesso");
    }
}
