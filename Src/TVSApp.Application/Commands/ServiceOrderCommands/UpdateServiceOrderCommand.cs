using TVS_App.Application.Exceptions;
using TVS_App.Domain.Enums;

namespace TVS_App.Application.Commands.ServiceOrderCommands;

public class UpdateServiceOrderCommand : ICommand
{
    public long CustomerId { get; set; }
    public long ServiceOrderId { get; set; }

    public string ProductBrand { get; set; } = string.Empty;
    public string ProductModel { get; set; } = string.Empty;
    public string ProductSerialNumber { get; set; } = string.Empty;
    public string ProductDefect { get; set; } = string.Empty;
    public string Accessories { get; set; } = string.Empty;
    public EProduct ProductType { get; set; }

    public EEnterprise Enterprise { get; set; } = EEnterprise.Particular;

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
    
    public void Normalize()
    {
        ProductBrand = ProductBrand.Trim().ToUpper();
        ProductModel = ProductModel.Trim().ToUpper();
        ProductSerialNumber = ProductSerialNumber.Trim().ToUpper();
        ProductDefect = ProductDefect.Trim().ToUpper();
        Accessories = Accessories.Trim().ToUpper();
    }
}