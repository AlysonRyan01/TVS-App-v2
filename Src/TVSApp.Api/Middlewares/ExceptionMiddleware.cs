using System.Text.Json;
using TVS_App.Application.Exceptions;
using TVS_App.Domain.Exceptions;
using TVS_App.Domain.Shared;

namespace TVS_App.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu uma exceção: {Message}", ex.Message);

            context.Response.ContentType = "application/json";

            int code;
            string message;

            switch (ex)
            {
                case var _ when ex is CommandException:
                case var _ when ex is ValueObjectException:
                case var _ when ex is EntityException:
                    code = 400;
                    message = ex.Message;
                    break;

                default:
                    code = 500;
                    message = _env.IsDevelopment()
                        ? $"Erro inesperado: {ex.Message}\n{ex.StackTrace}"
                        : ex.Message;
                    break;
            }

            var response = new BaseResponse<object?>(null, code, message);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(response, options);
            await context.Response.WriteAsync(json);
        }
    }
}