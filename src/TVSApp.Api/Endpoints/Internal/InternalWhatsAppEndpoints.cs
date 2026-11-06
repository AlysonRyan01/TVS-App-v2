using Microsoft.Extensions.Options;
using TVS_App.Api.Extensions;
using TVS_App.Api.Observability;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Observability;
using TVS_App.Domain.Shared;
using TVS_App.Infrastructure.Configurations;
using TVS_App.Infrastructure.Services.Whatsapp;

namespace TVS_App.Api.Endpoints.Internal;

/// <summary>
/// Rotas internas: o prefixo /internal só é acessível dentro da rede do Docker
/// (o Caddy responde 404 para /api/internal/* vindo da internet).
/// </summary>
public static class InternalWhatsAppEndpoints
{
    private const string MessageType = "TestMessage";

    public static void MapInternalWhatsAppEndpoints(this WebApplication app)
    {
        app.MapPost("/internal/whatsapp/test-message", async (
                IWhatsappService whatsapp,
                IOptions<WhatsAppTestSettings> settings,
                ILoggerFactory loggerFactory) =>
            {
                var phone = settings.Value.Phone;
                var result = await whatsapp.SendTestMessageAsync(phone);

                if (result.Code == 400)
                    return result.ToResult();

                var masked = LogMask.Phone(phone);
                var logger = loggerFactory.CreateLogger("WhatsAppTest");
                WhatsAppMetrics.RecordMessage(MessageType, result.IsSuccess);

                if (!result.IsSuccess)
                {
                    logger.LogWarning("WhatsApp falhou {MessageType:l} para {Phone:l}: {Reason:l}", MessageType, masked, result.Message);
                    return result.ToResult();
                }

                logger.LogInformation("WhatsApp enviado {MessageType:l} para {Phone:l}", MessageType, masked);
                return new BaseResponse<string>(result.Data, 200, $"Mensagem de teste enviada para {masked}.").ToResult();
            })
            .WithBusinessEvent(BusinessEvents.WhatsAppTest)
            .WithTags("Internal")
            .ExcludeFromDescription();
    }
}
