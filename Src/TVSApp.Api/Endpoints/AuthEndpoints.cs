using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using TVS_App.Application.DTOs.Auth;
using TVS_App.Domain.Shared;
using TVS_App.Infrastructure.Models;
using TVS_App.Infrastructure.Security;
using RefreshTokenRequest = TVS_App.Application.Requests.AuthRequests.RefreshTokenRequest;

namespace TVS_App.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/login", async (
                LoginRequest login,
                JwtService jwtService,
                UserManager<User> userManager,
                HttpContext httpContext) =>
        {
            var user = await userManager.FindByEmailAsync(login.Email);

            if (user == null || !await userManager.CheckPasswordAsync(user, login.Password))
            {
                return Results.Ok(new BaseResponse<AuthTokensDto>(null, 401, "Email ou senha inválidos"));
            }

            var accessToken = await jwtService.GenerateAccessToken(user);
            var refreshToken = await jwtService.GenerateRefreshTokenAsync(user, httpContext.Connection.RemoteIpAddress?.ToString());

            return Results.Ok(new BaseResponse<AuthTokensDto>(new AuthTokensDto(accessToken, refreshToken), 200, "Login realizado com sucesso!"));
        }).WithTags("Auth")
        .WithSummary("Autentica um usuário e gera um par de tokens (access + refresh).")
        .WithDescription("Recebe email e senha, verifica as credenciais e retorna um access token JWT de curta duração e um refresh token para renovação da sessão.")
        .Produces<BaseResponse<AuthTokensDto>>()
        .Produces<BaseResponse<AuthTokensDto>>(StatusCodes.Status401Unauthorized);

        app.MapPost("/register", async (
                LoginRequest login,
                JwtService jwtService,
                UserManager<User> userManager,
                HttpContext httpContext) =>
            {
                var existingUser = await userManager.FindByEmailAsync(login.Email);
                if (existingUser is not null)
                {
                    return Results.BadRequest(new BaseResponse<AuthTokensDto>(null, 401,
                        "Esse usuário já existe"));
                }

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

                var accessToken = await jwtService.GenerateAccessToken(user);
                var refreshToken = await jwtService.GenerateRefreshTokenAsync(user, httpContext.Connection.RemoteIpAddress?.ToString());

                return Results.Ok(new BaseResponse<AuthTokensDto>(new AuthTokensDto(accessToken, refreshToken), 200, "Usuário criado com sucesso!"));
            }).WithTags("Auth")
            .WithSummary(
                "Registra um novo usuário e retorna um par de tokens (access + refresh) ( Utilizado apenas por internos da empresa, por isso requer autorização ).")
            .WithDescription("Cria uma nova conta de usuário com base no email e senha fornecidos. "
                             + "Se o usuário já existir ou ocorrer erro na criação, retorna uma mensagem de erro.")
            .Produces<BaseResponse<AuthTokensDto>>(StatusCodes.Status200OK, "application/json")
            .Produces<BaseResponse<AuthTokensDto>>(StatusCodes.Status400BadRequest, "application/json")
            .Produces<BaseResponse<IEnumerable<string>>>(StatusCodes.Status500InternalServerError, "application/json")
            .RequireAuthorization();

        app.MapPost("/refresh-token", async (RefreshTokenRequest request, JwtService jwtService) =>
        {
            var accessToken = await jwtService.RefreshAccessTokenAsync(request.RefreshToken);

            if (accessToken is null)
                return Results.Ok(new BaseResponse<string>(null, 401, "Refresh token inválido ou expirado"));

            return Results.Ok(new BaseResponse<string>(accessToken, 200, "Token renovado com sucesso!"));
        }).WithTags("Auth")
        .WithSummary("Renova o access token a partir de um refresh token válido.")
        .WithDescription("Recebe um refresh token, valida se ainda está ativo e devolve um novo access token JWT.")
        .Produces<BaseResponse<string>>()
        .Produces<BaseResponse<string>>(StatusCodes.Status401Unauthorized);

        app.MapPost("/logout", async (RefreshTokenRequest request, JwtService jwtService) =>
        {
            await jwtService.RevokeRefreshTokenAsync(request.RefreshToken);

            return Results.Ok(new BaseResponse<string>(null, 200, "Logout realizado com sucesso!"));
        }).WithTags("Auth")
        .WithSummary("Revoga o refresh token do usuário, encerrando a sessão.")
        .WithDescription("Marca o refresh token informado como revogado, impedindo que ele seja usado para renovar o access token novamente.")
        .Produces<BaseResponse<string>>()
        .RequireAuthorization();
    }
}
