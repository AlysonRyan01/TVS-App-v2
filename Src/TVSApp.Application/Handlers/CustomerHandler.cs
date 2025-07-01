using TVS_App.Application.Commands;
using TVS_App.Application.Commands.CustomerCommands;
using TVS_App.Application.DTOs;
using TVS_App.Application.Interfaces.Handlers;
using TVS_App.Application.Mappers;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;
using TVS_App.Domain.ValueObjects.Customer;

namespace TVS_App.Application.Handlers;

public class CustomerHandler : ICustomerHandler
{
    private readonly ICustomerCommandRepository _customerCommandRepository;
    private readonly ICustomerQueryRepository _customerQueryRepository;

    public CustomerHandler(
        ICustomerCommandRepository customerCommandRepository, 
        ICustomerQueryRepository customerQueryRepository)
    {
        _customerCommandRepository = customerCommandRepository;
        _customerQueryRepository = customerQueryRepository;
    }

    public async Task<BaseResponse<CustomerDto?>> CreateCustomerAsync(CreateCustomerCommand command)
    {
        command.Normalize();
        command.Validate();

        var name = new Name(command.Name);
        var address = new Address(
            command.Street,
            command.Neighborhood,
            command.City,
            command.Number,
            command.ZipCode,
            command.State);
        var phone = new Phone(command.Phone);
        var phone2 = new Phone(command.Phone2);
        Email? email = string.IsNullOrWhiteSpace(command.Email) ? null : new Email(command.Email);
        var cpf = new Cpf(command.Cpf);

        var customer = new Customer(name, address, phone, phone2, email, cpf);

        await _customerCommandRepository.CreateAsync(customer);
            
        var dto = CustomerMapper.MapToDto(customer);
            
        return new BaseResponse<CustomerDto?>(dto, 200, "Cliente cadastrado com sucesso");
    }

    public async Task<BaseResponse<CustomerDto?>> UpdateCustomerAsync(UpdateCustomerCommand command)
    {
        command.Normalize();
        command.Validate();

        var existingCustomer = await _customerQueryRepository.GetByIdAsync(command.Id);

        if (!existingCustomer.IsSuccess || existingCustomer.Data is null)
            return new BaseResponse<CustomerDto?>(null, 404, "Cliente não encontrado.");

        var customer = existingCustomer.Data;

        customer.UpdateName(command.Name);
        customer.UpdateAdress(
            command.Street,
            command.Neighborhood,
            command.City,
            command.Number,
            command.ZipCode,
            command.State);
        customer.UpdatePhone(command.Phone, command.Phone2);
        customer.UpdateEmail(command.Email);
        customer.UpdateCpf(command.Cpf);

        await _customerCommandRepository.UpdateAsync(customer);
            
        var dto = CustomerMapper.MapToDto(customer);
            
        return new BaseResponse<CustomerDto?>(dto, 200, "Cliente atualizado com sucesso");
    }

    public async Task<BaseResponse<CustomerDto?>> GetCustomerByIdAsync(GetCustomerByIdCommand command)
    {
        command.Validate();

        var response = await _customerQueryRepository.GetByIdAsync(command.Id);
            
        if (!response.IsSuccess || response.Data == null)
            return new BaseResponse<CustomerDto?>(null, response.Code, response.Message);
            
        var dto = CustomerMapper.MapToDto(response.Data);
            
        return new BaseResponse<CustomerDto?>(dto, 200, "Cliente obtido com sucesso!");
    }
    
    public async Task<BaseResponse<List<CustomerDto>>> GetCustomerByNameAsync(string name)
    {
        var customersResponse = await _customerQueryRepository.GetCustomerByName(name);
        
        if (!customersResponse.IsSuccess || customersResponse.Data == null)
            return new BaseResponse<List<CustomerDto>>(null, 404, customersResponse.Message);
        
        var dto = CustomerMapper.MapToDtoList(customersResponse.Data);
        
        return new BaseResponse<List<CustomerDto>>(dto, 200, customersResponse.Message);
    }

    public async Task<BaseResponse<PaginatedResult<CustomerDto?>>> GetAllCustomersAsync(PaginationCommand command)
    {
        command.Validate();

        var response = await _customerQueryRepository.GetAllAsync(command.PageNumber, command.PageSize);
            
        if (!response.IsSuccess || response.Data == null || !response.Data.Items.Any())
            return new BaseResponse<PaginatedResult<CustomerDto?>>(null, 404, response.Message);
            
        var dto = CustomerMapper.MapToDtoList(response.Data.Items!);
            
        var paginatedResult = new PaginatedResult<CustomerDto?>(
            dto,
            response.Data.TotalCount,
            response.Data.PageNumber,
            response.Data.PageSize,
            response.Data.TotalPages);
            
        return new BaseResponse<PaginatedResult<CustomerDto?>>(paginatedResult, 200, "Clientes obtidos com sucesso!");
    }
}

