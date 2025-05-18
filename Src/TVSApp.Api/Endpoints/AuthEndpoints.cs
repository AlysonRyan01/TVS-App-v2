using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using TVS_App.Application.DTOs;
using TVS_App.Infrastructure.Models;
using TVS_App.Infrastructure.Security;

namespace TVS_App.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/login", async (LoginRequest login, JwtService jwtService, UserManager<User> userManager) =>
        {
            var user = await userManager.FindByEmailAsync(login.Email);

            if (user == null || !await userManager.CheckPasswordAsync(user, login.Password))
            {
                return Results.Unauthorized();
            }

            var token = await jwtService.Generate(user);

            return Results.Ok(new BaseResponse<string>(token, 200, "Token enviado com sucesso!"));
        }).WithTags("Auth");

        app.MapPost("/register", async (LoginRequest login, JwtService jwtService, UserManager<User> userManager) =>
        {
            var existingUser = await userManager.FindByEmailAsync(login.Email);
            if (existingUser is not null)
            {
                return Results.BadRequest(new BaseResponse<string>("Esse usuário já existe", 401, "Esse usuário já existe"));
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
                return Results.BadRequest(new BaseResponse<IEnumerable<string>>(errors, 500, "Ocorreu algum erro ao tentar se registrar"));
            }

            var token = await jwtService.Generate(user);

            return Results.Ok(new BaseResponse<string>(token, 200, "Usuário criado com sucesso!"));
        }).WithTags("Auth");

        app.MapGet("/test-auth", (ClaimsPrincipal user) =>
        {
            var name = user.Identity?.Name ?? "Sem nome";
            return Results.Ok($"Usuário autenticado: {name}");
        }).RequireAuthorization();
    }
}
    