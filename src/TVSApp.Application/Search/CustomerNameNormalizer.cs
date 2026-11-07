using System.Globalization;
using System.Text;

namespace TVS_App.Application.Search;

/// <summary>
/// Normaliza nomes de cliente para a busca da barra "Buscar cliente":
/// sem acento, maiúsculas e espaços colapsados. "%" e "_" ficam literais.
/// </summary>
public static class CustomerNameNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var lastWasSpace = true; // ignora espaços no início

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                    builder.Append(' ');
                lastWasSpace = true;
                continue;
            }

            builder.Append(c);
            lastWasSpace = false;
        }

        if (builder.Length > 0 && builder[^1] == ' ')
            builder.Length--;

        return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}
