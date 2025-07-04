using TVS_App.Application.Commands.CustomerCommands;
using TVS_App.Application.DTOs;
using TVS_App.Application.Mappers;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;
using TVS_App.Domain.ValueObjects.Customer;

namespace TVS_App.Application.Handlers.Customers;

public class CreateCustomerHandler : ICommandHandler<CreateCustomerCommand, BaseResponse<CustomerDto?>>
{
    private readonly ICustomerCommandRepository _customerCommandRepository;

    public CreateCustomerHandler(ICustomerCommandRepository customerCommandRepository)
    {
        _customerCommandRepository = customerCommandRepository;
    }

    public async Task<BaseResponse<CustomerDto?>> HandleAsync(CreateCustomerCommand command)
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
}