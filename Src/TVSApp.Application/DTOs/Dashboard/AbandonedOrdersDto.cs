namespace TVS_App.Application.DTOs.Dashboard;

public class AbandonedOrdersDto
{
    public List<AbandonedOrderDto> RepairedNotPickedUp { get; set; } = [];
    public List<AbandonedOrderDto> UnrepairedNotPickedUp { get; set; } = [];
}

public class AbandonedOrderDto
{
    public long Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
}
