using MediatR;
using TVS_App.Application.DTOs;
using TVS_App.Application.Exceptions;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.CustomerRequests.CustomerCommands;

public record CreateCustomerCommand(
    string Name,
    string Street,
    string Neighborhood,
    string City,
    string Number,
    string ZipCode,
    string State,
    string Phone,
    string Phone2,
    string Email,
    string Cpf
) : IRequest<BaseResponse<CustomerDto?>>
{
    public CreateCustomerCommand() : this(
        Name: string.Empty,
        Street: string.Empty,
        Neighborhood: string.Empty,
        City: "Campo Largo",
        Number: string.Empty,
        ZipCode: string.Empty,
        State: "PR",
        Phone: string.Empty,
        Phone2: string.Empty,
        Email: string.Empty,
        Cpf: string.Empty)
    { }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new CommandException<CreateCustomerCommand>("O nome do cliente não pode estar vazio");

        if (string.IsNullOrWhiteSpace(Phone))
            throw new CommandException<CreateCustomerCommand>("O telefone do cliente não pode estar vazio");
        
        if (Cpf.Length != 11)
            throw new CommandException<CreateCustomerCommand>("O CPF deve ter 11 caracteres");
    }

    public CreateCustomerCommand Normalize()
    {
        return this with
        {
            Name = Name.Trim().ToUpper(),
            Street = Street.Trim().ToUpper(),
            Neighborhood = Neighborhood.Trim().ToUpper(),
            City = City.Trim().ToUpper(),
            Number = Number.Trim().ToUpper(),
            ZipCode = ZipCode.Trim(),
            State = State.Trim().ToUpper(),
            Phone = Phone.Trim(),
            Phone2 = Phone2.Trim(),
            Email = Email.Trim().ToLower(),
            Cpf = Cpf.Replace(".", "").Replace("-", "").Trim()
        };
    }
}
