using TVS_App.Application.Search;

namespace TVS_App.Application.Interfaces.Services;

/// <summary>
/// Índice em memória com todos os clientes (id, nome, telefone) para a barra "Buscar cliente".
/// Todo código que grava (cria/edita/exclui) clientes MUST chamar <see cref="Invalidate"/> após gravar com sucesso.
/// </summary>
public interface ICustomerSearchCache
{
    /// <summary>Todas as entradas, ordenadas por Id decrescente (mais recente primeiro).</summary>
    Task<IReadOnlyList<CustomerSearchEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Descarta o índice; a próxima busca remonta a partir do banco.</summary>
    void Invalidate();
}
