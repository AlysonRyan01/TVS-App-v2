namespace TVS_App.Domain.Repositories.ServiceOrders;

public record ServiceOrderSearchFilter(
    long? Number,
    string? Customer,
    string? Model,
    string? SerialNumber,
    DateTime? From,
    DateTime? To);
