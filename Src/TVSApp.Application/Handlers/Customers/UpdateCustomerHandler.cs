using TVS_App.Application.Commands.CustomerCommands;
using TVS_App.Application.DTOs;
using TVS_App.Application.Mappers;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Customers;

public class UpdateCustomerHandler : ICommandHandler<UpdateCustomerCommand, BaseResponse<CustomerDto?>>
{
    private readonly ICustomerCommandRepository _customerCommandRepository;
    private readonly ICustomerQueryRepository _customerQueryRepository;

    public UpdateCustomerHandler(ICustomerCommandRepository customerCommandRepository, ICustomerQueryRepository customerQueryRepository)
    {
        _customerCommandRepository = customerCommandRepository;
        _customerQueryRepository = customerQueryRepository;
    }

    public async Task<BaseResponse<CustomerDto?>> HandleAsync(UpdateCustomerCommand command)
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
}