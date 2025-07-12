using TVS_App.Domain.Enums;

namespace TVSApp.Web.Models.ServiceOrders;

public class CreateServiceOrderCommandModel
{
    public long CustomerId { get; set; }
    public string ProductBrand { get; set; } = string.Empty;
    public string ProductModel { get; set; } = string.Empty;
    public string ProductSerialNumber { get; set; } = string.Empty;
    public string ProductDefect { get; set; } = string.Empty;
    public string Accessories { get; set; } = string.Empty;
    public EProduct ProductType { get; set; } = EProduct.Tv;
    public EEnterprise Enterprise { get; set; } = EEnterprise.Particular;
}