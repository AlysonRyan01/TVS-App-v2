namespace TVS_App.Application.Services;

public static class ValorPorExtenso
{
    private static readonly string[] Units =
    [
        "zero", "um", "dois", "três", "quatro", "cinco", "seis", "sete", "oito", "nove", "dez",
        "onze", "doze", "treze", "quatorze", "quinze", "dezesseis", "dezessete", "dezoito", "dezenove"
    ];

    private static readonly string[] Tens =
        ["", "", "vinte", "trinta", "quarenta", "cinquenta", "sessenta", "setenta", "oitenta", "noventa"];

    private static readonly string[] Hundreds =
    [
        "", "cento", "duzentos", "trezentos", "quatrocentos", "quinhentos",
        "seiscentos", "setecentos", "oitocentos", "novecentos"
    ];

    public static string Format(decimal value)
    {
        value = Math.Round(Math.Abs(value), 2);
        var reais = (long)Math.Truncate(value);
        var centavos = (int)((value - reais) * 100);

        if (reais == 0 && centavos == 0)
            return "zero real";

        var parts = new List<string>();
        if (reais > 0)
            parts.Add(FormatReais(reais));
        if (centavos > 0)
            parts.Add($"{Group(centavos)} {(centavos == 1 ? "centavo" : "centavos")}");

        return string.Join(" e ", parts);
    }

    private static string FormatReais(long reais)
    {
        var millions = (int)(reais / 1_000_000);
        var thousands = (int)(reais / 1_000 % 1_000);
        var rest = (int)(reais % 1_000);

        var groups = new List<(string Text, int Value)>();
        if (millions > 0)
            groups.Add(($"{Group(millions)} {(millions == 1 ? "milhão" : "milhões")}", millions));
        if (thousands > 0)
            groups.Add((thousands == 1 ? "mil" : $"{Group(thousands)} mil", thousands));
        if (rest > 0)
            groups.Add((Group(rest), rest));

        var text = groups[0].Text;
        for (var i = 1; i < groups.Count; i++)
        {
            var value = groups[i].Value;
            var joinWithE = value < 100 || value % 100 == 0;
            text += (joinWithE ? " e " : " ") + groups[i].Text;
        }

        var exactMillions = millions > 0 && thousands == 0 && rest == 0;
        var currency = reais == 1 ? "real" : "reais";
        return exactMillions ? $"{text} de {currency}" : $"{text} {currency}";
    }

    private static string Group(int value)
    {
        if (value == 100)
            return "cem";

        var parts = new List<string>();
        var hundreds = value / 100;
        var below = value % 100;

        if (hundreds > 0)
            parts.Add(Hundreds[hundreds]);

        if (below > 0)
        {
            if (below < 20)
                parts.Add(Units[below]);
            else
            {
                var tens = Tens[below / 10];
                var unit = below % 10;
                parts.Add(unit == 0 ? tens : $"{tens} e {Units[unit]}");
            }
        }

        return string.Join(" e ", parts);
    }
}
