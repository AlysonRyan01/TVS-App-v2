using MediatR;
using TVS_App.Application.DTOs;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.CustomerRequests.CustomerCommands;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Customers;

public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, BaseResponse<CustomerDto?>>
{
    private readonly ICustomerCommandRepository _customerCommandRepository;
    private readonly ICustomerQueryRepository _customerQueryRepository;

    public UpdateCustomerHandler(ICustomerCommandRepository customerCommandRepository, ICustomerQueryRepository customerQueryRepository)
    {
        _customerCommandRepository = customerCommandRepository;
        _customerQueryRepository = customerQueryRepository;
    }

    public async Task<BaseResponse<CustomerDto?>> Handle(
        UpdateCustomerCommand request, 
        CancellationToken cancellationToken = default)
    {
        var normalized = request.Normalize();
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
        customer.UpdateCpf(normalized.Cpf);

        await _customerCommandRepository.UpdateAsync(customer);
            
        var dto = CustomerMapper.MapToDto(customer);
            
        return new BaseResponse<CustomerDto?>(dto, 200, "Cliente atualizado com sucesso");
    }
}