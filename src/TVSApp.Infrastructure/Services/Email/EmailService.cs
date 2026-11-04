using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.EmailQueueRequests;
using TVS_App.Domain.Shared;
using TVS_App.Infrastructure.Configurations;

namespace TVS_App.Infrastructure.Services.Email;

public class EmailService : IEmailService
{
    private readonly string _toEmail = "tvseletronica@tvseletronica.com.br";
    private readonly string _toName = "Suporte TVS";
    private readonly string _fromEmail = "tvseletronica@tvseletronica.com.br";
    private readonly EmailServiceSettings _settings;
    
    public EmailService(IOptions<EmailServiceSettings> options)
    {
        _settings = options.Value;
    }

    public async Task<BaseResponse<string>> Send(
        SendEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var smtp = new SmtpClient();
            smtp.Host = _settings.Host;
            smtp.Port = _settings.Port;
            smtp.EnableSsl = true;
            smtp.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

            using var mail = new MailMessage();
            mail.From = new MailAddress(_fromEmail, request.Name);
            mail.Subject = "MENSAGEM RECEBIDA PELO SITE";
            mail.Body = request.Message;
            mail.IsBodyHtml = true;

            mail.To.Add(new MailAddress(_toEmail, _toName));
            
            if (request.Files != null)
            {
                foreach (var file in request.Files)
                {
                    var stream = file.OpenReadStream();
                    var mailAttachment = new Attachment(stream, file.FileName, file.ContentType);
                    mail.Attachments.Add(mailAttachment);
                }
            }

            await smtp.SendMailAsync(mail, cancellationToken);
            
            return new BaseResponse<string>("Mensagem enviada com sucesso!", 200, "Mensagem enviada com sucesso!");
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
                Console.WriteLine("Erro interno: " + ex.InnerException.Message);
            return new BaseResponse<string>("Erro ao enviar a mensagem!", 500, "Erro ao enviar a mensagem!");
        }
    }
}