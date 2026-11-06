namespace TVS_App.Domain.Repositories.ServiceOrders;

/// <summary>
/// Filtro da tela "Buscar OS": só um campo vem preenchido (o período From/To conta como um).
/// Chega normalizado pelo handler: textos sem espaços nas pontas e datas sem hora.
/// </summary>
public record ServiceOrderSearchFilter(
    long? Number,
    string? Customer,
    string? Model,
    string? SerialNumber,
    DateTime? From,
    DateTime? To);
