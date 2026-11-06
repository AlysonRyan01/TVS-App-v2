using TVS_App.Infrastructure.Security;

namespace TVS_App.Api.Common;

/// <summary>
/// Cookie da sessão de longa duração (refresh token). HttpOnly: scripts da página não leem;
/// Secure: só trafega em HTTPS (navegadores aceitam http://localhost); SameSite=Strict: outros sites
/// não conseguem usá-lo.
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "tvs_refresh";

    private static CookieOptions Options(DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        Expires = expires,
        IsEssential = true
    };

    public static void Set(HttpResponse response, string token) =>
        response.Cookies.Append(Name, token, Options(DateTimeOffset.UtcNow.Add(JwtService.RefreshTokenLifetime)));

    public static void Clear(HttpResponse response) =>
        response.Cookies.Delete(Name, Options(DateTimeOffset.UnixEpoch));

    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}
