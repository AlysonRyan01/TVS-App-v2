using TVS_App.Domain.Enums;

namespace TVS_App.Application.DTOs.ServiceOrder;

public class UpdateServiceOrderResponseDto
{
    public long Id { get; init; }
    public EServiceOrderStatus Status { get; init; }
    public ERepairStatus RepairStatus { get; init; }
    public decimal TotalAmount { get; init; }
}