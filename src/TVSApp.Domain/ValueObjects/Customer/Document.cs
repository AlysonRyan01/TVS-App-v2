using System.Text.RegularExpressions;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Exceptions;

namespace TVS_App.Domain.ValueObjects.Customer;

/// <summary>
/// Documento do cliente: CPF (11 dígitos) ou CNPJ (14 caracteres; as 12 primeiras posições podem ter
/// letras, no formato alfanumérico em vigor desde julho de 2026, e as 2 últimas são dígitos).
/// O tipo é deduzido pelo tamanho e não é gravado no banco.
/// </summary>
public partial class Document : ValueObject
{
    public const string InvalidMessage = "Informe um CPF (11 dígitos) ou um CNPJ (14 caracteres).";

    protected Document() { }

    public Document(string number)
    {
        var normalized = Normalize(number);

        if (!CpfPattern().IsMatch(normalized) && !CnpjPattern().IsMatch(normalized))
            throw new ValueObjectException<Document>(InvalidMessage);

        Number = normalized;
    }

    public string Number { get; private set; } = string.Empty;

    public EDocumentType Type => Number.Length == 11 ? EDocumentType.Cpf : EDocumentType.Cnpj;

    /// <summary>Documento vazio ou só com espaços vira "sem documento" (null).</summary>
    public static Document? FromOptional(string? number)
        => string.IsNullOrWhiteSpace(number) ? null : new Document(number);

    public override string ToString() => Type == EDocumentType.Cpf
        ? $"{Number[..3]}.{Number[3..6]}.{Number[6..9]}-{Number[9..]}"
        : $"{Number[..2]}.{Number[2..5]}.{Number[5..8]}/{Number[8..12]}-{Number[12..]}";

    private static string Normalize(string number)
        => (number ?? string.Empty)
            .Replace(".", "").Replace("-", "").Replace("/", "").Replace(" ", "")
            .Trim()
            .ToUpperInvariant();

    [GeneratedRegex(@"^\d{11}$")]
    private static partial Regex CpfPattern();

    [GeneratedRegex(@"^[A-Z0-9]{12}\d{2}$")]
    private static partial Regex CnpjPattern();
}
