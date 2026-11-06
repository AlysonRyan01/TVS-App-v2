using TVS_App.Application.DTOs;
using TVS_App.Application.Exceptions;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.CustomerRequests.CustomerCommands;

public record UpdateCustomerCommand(
    long Id,
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
    string? Document
)
{
    public void Validate()
    {
        if (Id == 0)
            throw new CommandException<UpdateCustomerCommand>("O Id do cliente não pode ser 0");

        if (string.IsNullOrWhiteSpace(Name))
            throw new CommandException<UpdateCustomerCommand>("O nome do cliente não pode estar vazio");

        if (string.IsNullOrWhiteSpace(Phone))
            throw new CommandException<UpdateCustomerCommand>("O telefone do cliente não pode estar vazio");
    }

    public UpdateCustomerCommand Normalize()
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
            Document = Document?.Trim()
        };
    }
}