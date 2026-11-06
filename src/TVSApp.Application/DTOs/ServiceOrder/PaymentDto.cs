namespace TVS_App.Application.DTOs.ServiceOrder;

public record PaymentDto(long Id, decimal Amount, DateTime PaidAt);
