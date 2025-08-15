using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.EmailQueueRequests;
using TVS_App.Domain.Shared;

namespace TVS_App.Api.Endpoints;

public static class EmailEndpoints
{
    public static void MapEmailEndpoints(this WebApplication app)
    {
        app.MapPost("/v1/email/send", async (HttpRequest httpRequest, IEmailQueue emailQueue) =>
            {
                var form = await httpRequest.ReadFormAsync();

                var request = new SendEmailRequest(
                    form["name"]!, 
                    form["email"]!, 
                    form["phone"]!, 
                    form["message"]!,
                    form.Files.ToList());

                var success = emailQueue.Enqueue(request);

                if (!success)
                    return Results.StatusCode(500);

                return Results.Ok(new BaseResponse<string>("E-mail enfileirado com sucesso", 200));
            })
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<BaseResponse<string>>(StatusCodes.Status200OK)
            .WithName("EnqueueEmail")
            .WithTags("Email");
    }
}