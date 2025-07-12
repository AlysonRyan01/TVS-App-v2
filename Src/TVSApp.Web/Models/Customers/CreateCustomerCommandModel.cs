namespace TVSApp.Web.Models.Customers;

public class CreateCustomerCommandModel
{
    public string Name { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = "Campo Largo";
    public string Number { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Phone2 { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
}