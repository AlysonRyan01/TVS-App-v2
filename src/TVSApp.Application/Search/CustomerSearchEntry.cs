namespace TVS_App.Application.Search;

/// <summary>Resumo imutável de um cliente no índice de busca em memória.</summary>
public sealed record CustomerSearchEntry(long Id, string Name, string Phone, string SearchKey)
{
    public static CustomerSearchEntry Create(long id, string? name, string? phone)
        => new(id, name ?? string.Empty, phone ?? string.Empty, CustomerNameNormalizer.Normalize(name));
}
