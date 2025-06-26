using System.Text.Json;
using TVS_App.Application.Commands.CustomerCommands;
using TVS_App.Application.Exceptions;
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
            context.Response.StatusCode = 200; // manter 200 para manter o contrato com o frontend

            int code = 500;
            string message;

            switch (ex)
            {
                case CommandException<CreateCustomerCommand> e:
                    code = 400;
                    message = e.Message;
                    break;

                case CommandException<UpdateCustomerCommand> e:
                    code = 400;
                    message = e.Message;
                    break;

                default:
                    code = 500;
                    message = _env.IsDevelopment()
                        ? $"{ex.Message}\n{ex.StackTrace}"
                        : "Ocorreu um erro inesperado.";
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