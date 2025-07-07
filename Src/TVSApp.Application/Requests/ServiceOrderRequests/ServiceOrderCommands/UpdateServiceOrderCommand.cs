using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Exceptions;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;

public record UpdateServiceOrderCommand(
    long CustomerId,
    long ServiceOrderId,
    string ProductBrand,
    string ProductModel,
    string ProductSerialNumber,
    string ProductDefect,
    string Accessories,
    EProduct ProductType,
    EEnterprise Enterprise = EEnterprise.Particular
) : IRequest<BaseResponse<UpdateServiceOrderResponseDto?>>
{
    public void Validate()
    {
        if (ServiceOrderId == 0)
            throw new CommandException<UpdateServiceOrderCommand>("O ServiceOrderId da ordem de serviço não pode ser 0");

        if (CustomerId == 0)
            throw new CommandException<UpdateServiceOrderCommand>("O CustomerId da ordem de serviço não pode ser 0");

        if (string.IsNullOrEmpty(ProductModel))
            throw new CommandException<UpdateServiceOrderCommand>("O modelo do produto da ordem de serviço não pode estar vazio");

        if (string.IsNullOrEmpty(ProductSerialNumber))
            throw new CommandException<UpdateServiceOrderCommand>("O número de série do produto da ordem de serviço não pode estar vazio");

        if (!Enum.IsDefined(typeof(EProduct), ProductType))
            throw new CommandException<UpdateServiceOrderCommand>("O tipo de produto da ordem de serviço é inválido.");

        if (!Enum.IsDefined(typeof(EEnterprise), Enterprise))
            throw new CommandException<UpdateServiceOrderCommand>("A empresa informada da ordem de serviço é inválida.");
    }

    public UpdateServiceOrderCommand Normalize()
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
