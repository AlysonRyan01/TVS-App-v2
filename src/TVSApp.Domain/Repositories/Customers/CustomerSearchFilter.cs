namespace TVS_App.Domain.Repositories.Customers;

/// <summary>
/// Filtros combinados (todos precisam corresponder) da busca de clientes. Valores já normalizados:
/// <paramref name="PhoneDigits"/> e <paramref name="DocumentDigits"/> sem pontuação.
/// </summary>
public record CustomerSearchFilter(string? Name, string? PhoneDigits, string? DocumentDigits);
