namespace TVS_App.Application.Observability;

public static class LogMask
{
    public static string Phone(string? phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return string.Empty;
        if (digits.Length < 6)
            return "***";

        return digits[..2] + new string('*', digits.Length - 6) + digits[^4..];
    }

    public static string Email(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        var at = email.IndexOf('@');
        if (at < 1)
            return "***";

        return email[0] + "***" + email[at..];
    }
}
