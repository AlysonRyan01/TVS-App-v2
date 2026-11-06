namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;

public record ServiceOrderSearchQuery(
    long? Number,
    string? Customer,
    string? Model,
    string? SerialNumber,
    DateTime? From,
    DateTime? To,
    int PageNumber = 1,
    int PageSize = 20);
