namespace TVSApp.Web.Models.ServiceOrders;

public class AddProductLocationCommandModel
{
    public long ServiceOrderId { get; set; }
    public string Location { get; set; } = string.Empty;
}