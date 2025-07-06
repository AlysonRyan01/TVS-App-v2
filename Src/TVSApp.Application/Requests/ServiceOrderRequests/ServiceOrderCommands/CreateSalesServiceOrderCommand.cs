using TVS_App.Application.Exceptions;
using TVS_App.Domain.Enums;

namespace TVS_App.Application.Commands.ServiceOrderCommands;

public class CreateSalesServiceOrderCommand
{
    public long CustomerId { get; set; }

    public string ProductBrand { get; set; } = string.Empty;
    public string ProductModel { get; set; } = string.Empty;
    public string ProductSerialNumber { get; set; } = string.Empty;
    public string Guarantee { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public EProduct ProductType { get; set; } = EProduct.Tv;

    public void Validate()
    {
        if (CustomerId == 0)
            throw new CommandException<CreateServiceOrderCommand>("O CustomerId da ordem de serviço não pode ser 0");

        if (string.IsNullOrEmpty(ProductModel))
            throw new CommandException<CreateServiceOrderCommand>("O modelo da ordem de serviço não pode estar vazio");

        if (string.IsNullOrEmpty(ProductSerialNumber))
            throw new CommandException<CreateServiceOrderCommand>("O número de série da ordem de serviço não pode estar vazio");
        
        if (string.IsNullOrEmpty(Guarantee))
            throw new CommandException<CreateServiceOrderCommand>("A venda precisa ter uma garantia");

        if (!Enum.IsDefined(typeof(EProduct), ProductType))
            throw new CommandException<CreateServiceOrderCommand>("O tipo do produto da ordem de serviço não tem um valor válido");
        
        if (Amount == 0)
            throw new CommandException<CreateServiceOrderCommand>("O valor da venda não pode ser vazio ou 0");
    }

    public void Normalize()
    {
        ProductBrand = ProductBrand.Trim().ToUpper();
        ProductModel = ProductModel.Trim().ToUpper();
        ProductSerialNumber = ProductSerialNumber.Trim().ToUpper();
        Guarantee = Guarantee.Trim().ToUpper();
    }
}