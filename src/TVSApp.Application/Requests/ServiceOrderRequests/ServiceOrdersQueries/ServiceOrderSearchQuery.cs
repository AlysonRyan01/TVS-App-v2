namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;

/// <summary>Busca da tela "Buscar OS": um filtro por vez (o período From/To conta como um).</summary>
public record ServiceOrderSearchQuery(
    long? Number,
    string? Customer,
    string? Model,
    string? SerialNumber,
    DateTime? From,
    DateTime? To,
    int PageNumber = 1,
    int PageSize = 20);
