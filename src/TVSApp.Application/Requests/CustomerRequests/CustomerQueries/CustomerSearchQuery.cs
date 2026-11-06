namespace TVS_App.Application.Requests.CustomerRequests.CustomerQueries;

/// <summary>Query string de GET /customers/search. Filtros opcionais e combinados.</summary>
public record CustomerSearchQuery(
    string? Name,
    string? Phone,
    string? Document,
    int PageNumber = 1,
    int PageSize = 20);
