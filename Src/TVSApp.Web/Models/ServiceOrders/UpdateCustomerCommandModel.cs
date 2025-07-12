namespace TVSApp.Web.Models.ServiceOrders;

public class UpdateCustomerCommandModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Street { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? Number { get; set; }
    public string? ZipCode { get; set; }
    public string? State { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Phone2 { get; set; }
    public string? Email { get; set; }
    public string? Cpf { get; set; }
}