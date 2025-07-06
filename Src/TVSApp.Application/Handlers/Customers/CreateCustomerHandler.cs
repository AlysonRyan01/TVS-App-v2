using MediatR;
using TVS_App.Application.DTOs;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.CustomerRequests.CustomerCommands;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;
using TVS_App.Domain.ValueObjects.Customer;

namespace TVS_App.Application.Handlers.Customers;

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, BaseResponse<CustomerDto?>>
{
    private readonly ICustomerCommandRepository _customerCommandRepository;

    public CreateCustomerHandler(ICustomerCommandRepository customerCommandRepository)
    {
        _customerCommandRepository = customerCommandRepository;
    }
    
    public async Task<BaseResponse<CustomerDto?>> Handle(
        CreateCustomerCommand request, 
        CancellationToken cancellationToken = default)
    {
        var normalized = request.Normalize();
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
        var cpf = new Cpf(normalized.Cpf);

        var customer = new Customer(name, address, phone, phone2, email, cpf);

        await _customerCommandRepository.CreateAsync(customer);
            
        var dto = CustomerMapper.MapToDto(customer);
            
        return new BaseResponse<CustomerDto?>(dto, 200, "Cliente cadastrado com sucesso");
    }
}