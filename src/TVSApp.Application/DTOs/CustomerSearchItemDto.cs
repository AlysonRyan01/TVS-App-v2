namespace TVS_App.Application.DTOs;

/// <summary>Item da busca rápida de clientes (GET /customers?name=).</summary>
public record CustomerSearchItemDto(long Id, string Name, string Phone);
