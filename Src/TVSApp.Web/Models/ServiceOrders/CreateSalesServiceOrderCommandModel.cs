using TVS_App.Domain.Enums;

namespace TVSApp.Web.Models.ServiceOrders;

public class CreateSalesServiceOrderCommandModel
{
    public long CustomerId { get; set; }
    public string ProductBrand { get; set; } = string.Empty;
    public string ProductModel { get; set; } = string.Empty;
    public string ProductSerialNumber { get; set; } = string.Empty;
    public string Guarantee { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public EProduct ProductType { get; set; } = EProduct.Tv;
}