using TVS_App.Application.Commands;
using TVS_App.Application.Commands.CustomerCommands;
using TVS_App.Application.DTOs;
using TVS_App.Application.Exceptions;
using TVS_App.Application.Repositories;
using TVS_App.Domain.Entities;
using TVS_App.Domain.ValueObjects.Customer;

namespace TVS_App.Application.Handlers;

public class CustomerHandler
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<BaseResponse<Customer?>> CreateCustomerAsync(CreateCustomerCommand command)
    {
        try
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
            var email = new Email(command.Email);

            var customer = new Customer(name, address, phone, phone2, email);

            return await _customerRepository.CreateAsync(customer);
        }
        catch (CommandException<CreateCustomerCommand> ex)
        {
            return new BaseResponse<Customer?>(null, 400, $"Erro de validação: {ex.Message}");
        }
    }

    public async Task<BaseResponse<Customer?>> UpdateCustomerAsync(UpdateCustomerCommand command)
    {
        try
        {
            command.Normalize();
            command.Validate();

            var existingCustomer = await _customerRepository.GetByIdAsync(command.Id);

            if (!existingCustomer.IsSuccess || existingCustomer.Data is null)
                return new BaseResponse<Customer?>(null, 404, "Cliente não encontrado.");

            var customer = existingCustomer.Data;

            customer.UpdateName(command.Name);
            customer.UpdateAdress(command.Street,
                command.Neighborhood,
                command.City,
                command.Number,
                command.ZipCode,
                command.State);
            customer.UpdatePhone(command.Phone, command.Phone2);
            customer.UpdateEmail(command.Email);

            return await _customerRepository.UpdateAsync(customer);
        }
        catch (CommandException<UpdateCustomerCommand> ex)
        {
            return new BaseResponse<Customer?>(null, 400, $"Erro de validação: {ex.Message}");
        }
    }

    public async Task<BaseResponse<Customer?>> GetCustomerByIdAsync(GetCustomerByIdCommand command)
    {
        try
        {
            command.Validate();

            return await _customerRepository.GetByIdAsync(command.Id);
        }
        catch (CommandException<GetCustomerByIdCommand> ex)
        {
            return new BaseResponse<Customer?>(null, 400, $"Erro de validação: {ex.Message}");
        }
    }
    
    public async Task<BaseResponse<List<Customer>>> GetCustomerByNameAsync(string name)
    {
        return await _customerRepository.GetCustomerByName(name);
    }

    public async Task<BaseResponse<PaginatedResult<Customer?>>> GetAllCustomersAsync(PaginationCommand command)
    {
        try
        {
            command.Validate();

            return await _customerRepository.GetAllAsync(command.PageNumber, command.PageSize);
        }
        catch (CommandException<PaginationCommand> ex)
        {
            return new BaseResponse<PaginatedResult<Customer?>>(null, 400, $"Erro de validação: {ex.Message}");
        }
    }
}

