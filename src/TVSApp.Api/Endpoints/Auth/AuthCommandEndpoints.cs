using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using TVS_App.Api.Common;
using TVS_App.Api.Observability;
using TVS_App.Application.DTOs.Auth;
using TVS_App.Application.Observability;
using TVS_App.Domain.Shared;
using TVS_App.Infrastructure.Models;
using TVS_App.Infrastructure.Security;

namespace TVS_App.Api.Endpoints.Auth;

/// <summary>
/// Autenticação: o token de acesso (1 h) vai no corpo e fica só em memória no navegador;
/// a sessão de 30 dias (refresh token) vive apenas no cookie HttpOnly <see cref="RefreshTokenCookie"/>.
/// </summary>
public static class AuthCommandEndpoints
{
    private const string SessionExpired = "Sessão expirada. Entre novamente.";

    public static void MapAuthCommandEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (
            LoginRequest login,
            JwtService jwtService,
            UserManager<User> userManager,
            HttpContext httpContext,
            ILoggerFactory loggerFactory) =>
        {
            var user = await userManager.FindByEmailAsync(login.Email);

            if (user == null || !await userManager.CheckPasswordAsync(user, login.Password))
            {
                loggerFactory.CreateLogger("Auth")
                    .LogWarning("Falha de login para {Email:l}", LogMask.Email(login.Email));
                return Results.BadRequest(new BaseResponse<AccessTokenDto>(null, 401, "Email ou senha inválidos"));
            }

            var accessToken = await jwtService.GenerateAccessToken(user);
            var refreshToken =
                await jwtService.GenerateRefreshTokenAsync(user, httpContext.Connection.RemoteIpAddress?.ToString());

            RefreshTokenCookie.Set(httpContext.Response, refreshToken);

            return Results.Ok(new BaseResponse<AccessTokenDto>(new AccessTokenDto(accessToken), 200,
                "Login realizado com sucesso!"));
        }).WithBusinessEvent(BusinessEvents.AuthLogin).WithTags("Auth");

        app.MapPost("/auth/logout", async (HttpContext httpContext, JwtService jwtService) =>
        {
            var refreshToken = RefreshTokenCookie.Read(httpContext.Request);
            if (refreshToken is not null)
                await jwtService.RevokeRefreshTokenAsync(refreshToken);

            RefreshTokenCookie.Clear(httpContext.Response);
            return Results.Ok(new BaseResponse<string>(null, 200, "Logout realizado com sucesso!"));
        }).WithTags("Auth");

        app.MapPost("/auth/refresh", async (HttpContext httpContext, JwtService jwtService) =>
        {
            var refreshToken = RefreshTokenCookie.Read(httpContext.Request);
            var accessToken = refreshToken is null ? null : await jwtService.RefreshAccessTokenAsync(refreshToken);

            if (accessToken is null)
            {
                RefreshTokenCookie.Clear(httpContext.Response);
                return Results.Json(new BaseResponse<string>(null, 401, SessionExpired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(new BaseResponse<string>(accessToken, 200, "Token renovado com sucesso!"));
        }).WithTags("Auth");

        app.MapPost("/auth/register", async (
                LoginRequest login,
                UserManager<User> userManager) =>
            {
                var existingUser = await userManager.FindByEmailAsync(login.Email);
                if (existingUser is not null)
                    return Results.BadRequest(new BaseResponse<string>(null, 401, "Esse usuário já existe"));

                var user = new User
                {
                    UserName = login.Email,
                    Email = login.Email
                };

                var result = await userManager.CreateAsync(user, login.Password);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description);
                    return Results.BadRequest(new BaseResponse<IEnumerable<string>>(errors, 500,
                        "Ocorreu algum erro ao tentar se registrar"));
                }

                return Results.Ok(new BaseResponse<string>(null, 200, "Usuário criado com sucesso!"));
            }).WithTags("Auth")
            .RequireAuthorization();
    }
}
