using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using TVS_App.Domain.Shared;
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
                return Results.Ok(new BaseResponse<string>(null, 401, "Email ou senha inválidos"));
            }

            var token = await jwtService.Generate(user);

            return Results.Ok(new BaseResponse<string>(token, 200, "Token enviado com sucesso!"));
        }).WithTags("Auth")
        .WithSummary("Autentica um usuário e gera um token JWT.")
        .WithDescription("Recebe email e senha, verifica as credenciais e retorna um token JWT para autenticação.")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK)
        .Produces<BaseResponse<string>>(StatusCodes.Status401Unauthorized);

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
        }).WithTags("Auth")
        .WithSummary("Registra um novo usuário e retorna um token JWT ( Utilizado apenas por internos da empresa, por isso requer autorização ).")
        .WithDescription("Cria uma nova conta de usuário com base no email e senha fornecidos. "
                         + "Se o usuário já existir ou ocorrer erro na criação, retorna uma mensagem de erro.")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .Produces<BaseResponse<IEnumerable<string>>>(StatusCodes.Status500InternalServerError, "application/json")
        .RequireAuthorization();
    }
}
    