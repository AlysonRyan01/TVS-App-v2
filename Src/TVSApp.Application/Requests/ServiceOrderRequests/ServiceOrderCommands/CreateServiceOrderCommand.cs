using MediatR;
using TVS_App.Application.Exceptions;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;

public record CreateServiceOrderCommand(
    long CustomerId,
    string ProductBrand,
    string ProductModel,
    string ProductSerialNumber,
    string ProductDefect,
    string Accessories,
    EProduct ProductType = EProduct.Tv,
    EEnterprise Enterprise = EEnterprise.Particular
) : IRequest<BaseResponse<byte[]>>
{
    public void Validate()
    {
        if (CustomerId == 0)
            throw new CommandException<CreateServiceOrderCommand>("O CustomerId da ordem de serviço não pode ser 0");

        if (string.IsNullOrEmpty(ProductModel))
            throw new CommandException<CreateServiceOrderCommand>("O modelo da ordem de serviço não pode estar vazio");

        if (string.IsNullOrEmpty(ProductSerialNumber))
            throw new CommandException<CreateServiceOrderCommand>("O número de série da ordem de serviço não pode estar vazio");

        if (!Enum.IsDefined(typeof(EProduct), ProductType))
            throw new CommandException<CreateServiceOrderCommand>("O tipo do produto da ordem de serviço não tem um valor válido");

        if (!Enum.IsDefined(typeof(EEnterprise), Enterprise))
            throw new CommandException<CreateServiceOrderCommand>("A empresa da ordem de serviço não tem um valor válido");
    }

    public CreateServiceOrderCommand Normalize()
    {
        return this with
        {
            ProductBrand = ProductBrand.Trim().ToUpper(),
            ProductModel = ProductModel.Trim().ToUpper(),
            ProductSerialNumber = ProductSerialNumber.Trim().ToUpper(),
            ProductDefect = ProductDefect.Trim().ToUpper(),
            Accessories = Accessories.Trim().ToUpper()
        };
    }
}