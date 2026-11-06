using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.Extensions;
using TVS_App.Api.SignalR;
using TVS_App.Application.Handlers;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Application.Requests.WhatsAppQueueRequests;
using TVS_App.Domain.Shared;

namespace TVS_App.Api.Endpoints.Public;

public static class PublicCommandEndpoints
{
    private const long MaxTotalBytes = 25 * 1024 * 1024;
    private const int MaxFiles = 10;

    public static void MapPublicCommandEndpoints(this WebApplication app)
    {
        app.MapPut("/add-service-order-approve-estimate", async (
                IServiceOrderCommandHandler handler,
                CustomerEstimateResponseRequest request,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.ApproveEstimateByCustomer(request.Id, request.Code);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithTags("Public");

        app.MapPut("/add-service-order-reject-estimate", async (
                IServiceOrderCommandHandler handler,
                CustomerEstimateResponseRequest request,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.RejectEstimateByCustomer(request.Id, request.Code);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithTags("Public");

        app.MapPost("/v1/whatsapp/site-contact", async (HttpRequest httpRequest, IWhatsAppQueue whatsAppQueue) =>
            {
                if (!httpRequest.HasFormContentType)
                    return Results.BadRequest(new BaseResponse<string>(null, 400, "Formato inválido."));

                var form = await httpRequest.ReadFormAsync();

                var name = form["name"].ToString().Trim();
                var phone = form["phone"].ToString().Trim();
                var email = form["email"].ToString().Trim();
                var message = form["message"].ToString().Trim();
                var subject = form["subject"].ToString().Trim();

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
                    return Results.BadRequest(new BaseResponse<string>(null, 400, "Nome e telefone são obrigatórios."));

                if (name.Length > 120 || phone.Length > 30 || email.Length > 200 || subject.Length > 60 || message.Length > 4000)
                    return Results.BadRequest(new BaseResponse<string>(null, 400, "Campos excedem o tamanho permitido."));

                if (form.Files.Count > MaxFiles)
                    return Results.BadRequest(new BaseResponse<string>(null, 400, $"Máximo de {MaxFiles} arquivos."));

                if (form.Files.Sum(f => f.Length) > MaxTotalBytes)
                    return Results.BadRequest(new BaseResponse<string>(null, 400, "Os anexos excedem 25 MB no total."));

                var attachments = new List<WhatsAppMediaAttachment>();
                foreach (var file in form.Files)
                {
                    var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
                    if (!contentType.StartsWith("image/") && !contentType.StartsWith("video/"))
                        return Results.BadRequest(new BaseResponse<string>(null, 400, $"Tipo de arquivo não permitido: {file.FileName}"));

                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    attachments.Add(new WhatsAppMediaAttachment(
                        Path.GetFileName(file.FileName),
                        contentType,
                        Convert.ToBase64String(ms.ToArray())));
                }

                var request = new SendSiteContactMessageRequest(
                    string.IsNullOrWhiteSpace(subject) ? "Contato via site" : subject,
                    name,
                    phone,
                    string.IsNullOrWhiteSpace(email) ? null : email,
                    string.IsNullOrWhiteSpace(message) ? null : message,
                    attachments);

                if (!whatsAppQueue.Enqueue(request))
                    return Results.StatusCode(500);

                return Results.Ok(new BaseResponse<string>("Mensagem enfileirada com sucesso", 200));
            })
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<BaseResponse<string>>(StatusCodes.Status200OK)
            .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest)
            .WithName("SendSiteContactWhatsApp")
            .WithSummary("Recebe o formulário do site e encaminha ao WhatsApp da loja (texto + fotos/vídeos).")
            .WithTags("WhatsApp");
    }
}
