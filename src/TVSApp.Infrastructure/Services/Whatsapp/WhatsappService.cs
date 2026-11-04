using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.WhatsAppQueueRequests;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;
using TVS_App.Infrastructure.Configurations;
using TVS_App.Infrastructure.Extensions;

namespace TVS_App.Infrastructure.Services.Whatsapp;

public class WhatsappService : IWhatsappService
{
    private readonly HttpClient _httpClient;
    private readonly EvolutionApiSettings _settings;
    private readonly IWebHostEnvironment _env;

    public WhatsappService(
        HttpClient httpClient, 
        IOptions<EvolutionApiSettings> options,
        IWebHostEnvironment env)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _env = env;
    }
    
    public async Task<BaseResponse<string>> SendWelcomeMessage(string serviceOrder, string customerName, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(serviceOrder))
            return new BaseResponse<string>(null, 404, "Ordem de serviço vazia");
            
        if (string.IsNullOrWhiteSpace(customerName))
            return new BaseResponse<string>(null, 404, "Nome do cliente vazio");
            
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return new BaseResponse<string>(null, 404, "Número de telefone vazio");

        if (_env.IsDevelopment())
            phoneNumber = "5541997561468";

        var text = $"*Olá {customerName}, seja bem-vindo(a) à TVS Eletrônica! 👋😊*\n\n" +
                   $"*📄 Recebemos sua ordem de serviço nº {serviceOrder} com sucesso ✅*.\n\n" +
                   $"🔍 Nossa equipe já está analisando seu aparelho 🛠️ e em breve entraremos em contato com mais informações.\n\n" +
                   $"🙏 Agradecemos a confiança!\n\n" +
                   $"Atenciosamente,\nEquipe TVS Eletrônica 📺🔧✨";

        return await Send(text, phoneNumber);
    }

    public async Task<BaseResponse<string>> SendEstimateMessage(
        string serviceOrder, 
        string securityCode, 
        ERepairResult? result, 
        string customerName, 
        string? solution, 
        string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(serviceOrder))
            return new BaseResponse<string>(null, 404, "Ordem de serviço vazia");
            
        if (string.IsNullOrWhiteSpace(securityCode))
            return new BaseResponse<string>(null, 404, "Código de segurança vazio");

        if (string.IsNullOrWhiteSpace(customerName))
            return new BaseResponse<string>(null, 404, "Nome do cliente vazio");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return new BaseResponse<string>(null, 404, "Número de telefone vazio");
            
        if (_env.IsDevelopment())
            phoneNumber = "5541997561468";
            
        string text;

        if (result == ERepairResult.Unrepaired || result == ERepairResult.NoDefectFound)
        {
            text = $"*Olá {customerName}, tudo bem? 👋*\n\n" +
                   $"*Sobre a ordem de serviço nº {serviceOrder}, informamos:*\n\n" +
                   $"🛠️ Situação: {result.GetDisplayName()}.\n" +
                   $"🔎 Detalhes: {solution}\n\n" +
                   $"Em caso de dúvidas, estamos à disposição.\n\n" +
                   $"Atenciosamente,\nTVS Eletrônica 📺🔧";
                
            return await Send(text, phoneNumber);
        }
            
        var link = $"https://www.tvseletronica.com/consultar?id={serviceOrder}&code={securityCode}";

        var text1 = $"*Olá {customerName}, tudo bem? 👋*\n\n" +
                    $"*O orçamento referente à sua Ordem de Serviço nº {serviceOrder} está disponível no link abaixo.*\n\n" +
                    $"*Nele, você pode visualizar os detalhes e aprovar ou recusar o orçamento diretamente.*";
                
        await Send(text1, phoneNumber);
        await Task.Delay(3000);
        var sendResult = await Send(link, phoneNumber);

        return sendResult;
    }
    
    public async Task<BaseResponse<string>> SendDeviceReadyMessage(string serviceOrder, string customerName, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(serviceOrder))
            return new BaseResponse<string>(null, 404, "Ordem de serviço vazia");
            
        if (string.IsNullOrWhiteSpace(customerName))
            return new BaseResponse<string>(null, 404, "Nome do cliente vazio");
            
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return new BaseResponse<string>(null, 404, "Número de telefone vazio");
            
        if (_env.IsDevelopment())
            phoneNumber = "5541997561468";

        var text = $"Olá {customerName}, tudo bem? 👋😊\n\n" +
                   $"📦 Sua ordem de serviço nº {serviceOrder} está pronta para retirada! 🎉✅\n" +
                   $"🧪 O seu aparelho já passou por todos os testes e consertos necessários 🛠️🔍.\n\n" +
                   $"🙏 Agradecemos por confiar na TVS Eletrônica. Estamos à disposição para qualquer dúvida! 💬\n\n" +
                   $"Atenciosamente,\nEquipe TVS Eletrônica 📺🔧✨";

        return await Send(text, phoneNumber);
    }

    public async Task<BaseResponse<string>> SendGuaranteeMessage(
        string serviceOrder, 
        string customerName, 
        string guarantee, 
        string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(serviceOrder))
            return new BaseResponse<string>(null, 404, "Ordem de serviço vazia");
            
        if (string.IsNullOrWhiteSpace(customerName))
            return new BaseResponse<string>(null, 404, "Nome do cliente vazio");
            
        if (string.IsNullOrWhiteSpace(guarantee))
            return new BaseResponse<string>(null, 404, "Garantia vazia");
            
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return new BaseResponse<string>(null, 404, "Número de telefone vazio");
            
        if (_env.IsDevelopment())
            phoneNumber = "5541997561468";

        var text = $"""
                    Olá {customerName}, tudo bem? 👋😊

                    Aqui é da TVS Eletrônica 📺🔧. Informamos que sua ordem de serviço nº {serviceOrder} foi finalizada ✅ e o produto já foi entregue 📬.

                    🛡️ Garantia: {guarantee}

                    🙏 Agradecemos pela confiança em nossos serviços! Qualquer dúvida ou necessidade, estamos à disposição 💬.

                    Atenciosamente,  
                    Equipe TVS Eletrônica ✨
                    """;

        return await Send(text, phoneNumber);
    }

    public async Task<BaseResponse<string>> SendProductSaleMessage(
        string customerName, 
        string phoneNumber, 
        decimal amount,
        string guarantee, 
        EProduct productType)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return new BaseResponse<string>(null, 404, "Nome do cliente vazio");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return new BaseResponse<string>(null, 404, "Número de telefone vazio");

        if (string.IsNullOrWhiteSpace(guarantee))
            return new BaseResponse<string>(null, 404, "Garantia vazia");
            
        if (_env.IsDevelopment())
            phoneNumber = "5541997561468";

        var formattedAmount = amount.ToString("C", new CultureInfo("pt-BR"));

        var text = $"Olá {customerName}, tudo bem? 👋\n\n" +
                   $"Agradecemos por adquirir um produto da TVS Eletrônica! 🛒\n\n" +
                   $"📦 Produto: {productType.ToString()}\n" +
                   $"💰 Valor: {formattedAmount}\n" +
                   $"🛡️ Garantia: {guarantee}\n\n" +
                   $"Em caso de dúvidas ou necessidade de suporte, estamos à disposição!\n\n" +
                   $"Atenciosamente,\nEquipe TVS Eletrônica 📺🔧";

        return await Send(text, phoneNumber);
    }

    public async Task<BaseResponse<string>> SendRejectionReadyForPickupMessage(string customerName, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return new BaseResponse<string>(null, 404, "Nome do cliente vazio");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return new BaseResponse<string>(null, 404, "Número de telefone vazio");
            
        if (_env.IsDevelopment())
            phoneNumber = "5541997561468";

        var text = $"🚀 *Pronto para retirada, {customerName}!*\n\n" +
                   $"Seu equipamento está disponível na TVS Eletrônica:\n" +
                   $"🕒 09h-12h 13-18h (Seg-Sex)\n\n" +
                   $"⚠️ *Lembrete:* Orçamento não foi aprovado\n\n" +
                   $"📞 Dúvidas? Chame aqui mesmo!";
            
        return await Send(text, phoneNumber);
    }

    public async Task<BaseResponse<string>> NotifyOwnerEstimateDecision(
        string serviceOrder, 
        ERepairStatus result, 
        string customerName)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return new BaseResponse<string>(null, 404, "Nome do cliente vazio");

        string formattedNumber = string.Empty;
            
        if (_env.IsDevelopment())
            formattedNumber = "41997561468";
        if (_env.IsProduction())
            formattedNumber = "41992744920";
            
        var decisionText = result switch
        {
            ERepairStatus.Approved => "aprovou",
            ERepairStatus.Disapproved => "rejeitou",
            _ => "tomou uma decisão sobre"
        };

        var text = $"📢 *DECISÃO DE ORÇAMENTO - ATUALIZAÇÃO* 📢\n\n" +
                   $"🛠️ *Ordem de Serviço:* #{serviceOrder}\n" +
                   $"👤 *Cliente:* {customerName}\n" +
                   $"📝 *Status:* {GetStatusEmoji(result)} {decisionText.ToUpper()} o orçamento\n";
            
        return await Send(text, formattedNumber);
    }

    public async Task<BaseResponse<string>> SendThankYouForEstimateApprovalAsync(
        string serviceOrder, 
        string customerName, 
        string phoneNumber,
        decimal amount)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return new BaseResponse<string>(null, 404, "Nome do cliente vazio");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return new BaseResponse<string>(null, 404, "Número de telefone vazio");
            
        if (_env.IsDevelopment())
            phoneNumber = "41997561468";
        
        var firstHalf = amount * 0.5m;
        var secondHalf = amount * 0.5m;

        var text = 
            $"Olá, {customerName}! 🙌\n\n" +
            $"A OS #{serviceOrder} foi *aprovada e está aguardando início*! Para começarmos o reparo, precisamos do pagamento de 50% (R$ {firstHalf:N2}).\n\n" +
            $"💰 *Valor total:* R$ {amount:N2}\n" +
            $"💳 *Agora (50%):* R$ {firstHalf:N2}\n" +
            $"📅 *Na retirada:* R$ {secondHalf:N2}\n\n" +
            $"*Qual sua forma de pagamento preferida?*\n" +
            $"• PIX\n" +
            $"• Cartão de crédito/débito\n" +
            $"• Dinheiro\n\n" +
            $"Te avisaremos assim que o reparo for concluído! Qualquer dúvida, pode chamar. 😊\n\n" +
            $"*Equipe TVS Eletrônica*";

        return await Send(text, phoneNumber);
    }

    public async Task<BaseResponse<string>> SendEstimateDisapprovalAcknowledgmentAsync(
        string serviceOrder, 
        string customerName, 
        string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return new BaseResponse<string>(null, 404, "Nome do cliente vazio");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return new BaseResponse<string>(null, 404, "Número de telefone vazio");
            
        if (_env.IsDevelopment())
            phoneNumber = "5541997561468";

        var text = $"✨ *{customerName}*, recebemos sua resposta! ✨\n\n" +
                   $"🔹 OS: #{serviceOrder}\n" +
                   $"🔹 Status: Orçamento não aprovado\n\n" +
                   $"⏳ *Avisaremos pelo WhatsApp* quando seu equipamento estiver pronto para retirada (em até 2 dias úteis)*\n" +
                   $"📞 Dúvidas? TVS Eletrônica à disposição!\n\n" +
                   $"💙 Agradecemos pela confiança!";
            
        return await Send(text, phoneNumber);
    }

    public async Task<BaseResponse<string>> Send(string message, string phoneNumber)
    {
        try
        {
            string formattedNumber = FormatPhoneNumber(phoneNumber);
            
            var urlEstimateText = $"{_settings.BaseUrl}/message/sendText/{_settings.Instance}";
            var requestBodyEstimateText = new
            {
                number = formattedNumber,
                text = message,
                delay = 0,
                linkPreview = true,
                mentionsEveryOne = false
            };
            var jsonEstimateText = JsonSerializer.Serialize(requestBodyEstimateText);
            var contentEstimateText = new StringContent(jsonEstimateText, Encoding.UTF8, "application/json");
            var requestEstimateText = new HttpRequestMessage(HttpMethod.Post, urlEstimateText);
            requestEstimateText.Headers.Add("apikey", _settings.ApiKey);
            requestEstimateText.Content = contentEstimateText;
            var responseEstimateText = await _httpClient.SendAsync(requestEstimateText);
            var responseEstimateTextContent = await responseEstimateText.Content.ReadAsStringAsync();

            if (!responseEstimateText.IsSuccessStatusCode)
                return new BaseResponse<string>(null, (int)responseEstimateText.StatusCode, $"Erro ao enviar mensagem de texto: {responseEstimateTextContent}");
            
            return new BaseResponse<string>(
                $"Mensagem enviada com sucesso!",
                200,
                "Mensagem enviada com sucesso!");
        }
        catch (HttpRequestException httpEx)
        {
            return new BaseResponse<string>(null, 500, httpEx.Message);
        }
        catch (TaskCanceledException taskCanceledEx)
        {
            return new BaseResponse<string>(null, 500, $"Ocorreu um erro: {taskCanceledEx.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<string>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }

    public async Task<BaseResponse<string>> NotifyOwnerSiteContactAsync(SendSiteContactMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return new BaseResponse<string>(null, 404, "Nome do cliente vazio");

        if (string.IsNullOrWhiteSpace(request.Phone))
            return new BaseResponse<string>(null, 404, "Número de telefone vazio");

        var ownerNumber = _env.IsProduction() ? "41992744920" : "41997561468";

        var sb = new StringBuilder();
        sb.Append($"📩 *{request.Subject.ToUpper()}* 📩\n\n");
        sb.Append($"👤 *Nome:* {request.Name}\n");
        sb.Append($"📞 *Telefone:* {request.Phone}\n");
        sb.Append($"💬 *WhatsApp:* https://wa.me/{FormatPhoneNumber(request.Phone)}\n");
        sb.Append($"✉️ *E-mail:* {(string.IsNullOrWhiteSpace(request.Email) ? "Não informado" : request.Email)}\n");

        if (!string.IsNullOrWhiteSpace(request.Message))
            sb.Append($"\n📝 *Mensagem:*\n{request.Message}\n");

        if (request.Attachments.Count > 0)
            sb.Append($"\n📎 {request.Attachments.Count} anexo(s) a seguir.");

        var textResult = await Send(sb.ToString(), ownerNumber);
        if (!textResult.IsSuccess)
            return textResult;

        var failures = 0;
        for (var i = 0; i < request.Attachments.Count; i++)
        {
            await Task.Delay(1500);
            var caption = $"{request.Name} — anexo {i + 1}/{request.Attachments.Count}";
            var mediaResult = await SendMedia(request.Attachments[i], caption, ownerNumber);
            if (!mediaResult.IsSuccess)
                failures++;
        }

        if (failures > 0)
            return new BaseResponse<string>(null, 500, $"{failures} anexo(s) não foram enviados.");

        return new BaseResponse<string>("Mensagem enviada com sucesso!", 200, "Mensagem enviada com sucesso!");
    }

    public async Task<BaseResponse<string>> SendMedia(WhatsAppMediaAttachment attachment, string caption, string phoneNumber)
    {
        try
        {
            var mediaType = attachment.MimeType switch
            {
                var m when m.StartsWith("image/") => "image",
                var m when m.StartsWith("video/") => "video",
                var m when m.StartsWith("audio/") => "audio",
                _ => "document"
            };

            var url = $"{_settings.BaseUrl}/message/sendMedia/{_settings.Instance}";
            var body = new
            {
                number = FormatPhoneNumber(phoneNumber),
                mediatype = mediaType,
                mimetype = attachment.MimeType,
                caption,
                media = attachment.Base64,
                fileName = attachment.FileName
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", _settings.ApiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return new BaseResponse<string>(null, (int)response.StatusCode, $"Erro ao enviar mídia: {content}");
            }

            return new BaseResponse<string>("Mídia enviada com sucesso!", 200, "Mídia enviada com sucesso!");
        }
        catch (Exception ex)
        {
            return new BaseResponse<string>(null, 500, $"Ocorreu um erro ao enviar mídia: {ex.Message}");
        }
    }

    public static string FormatPhoneNumber(string rawPhoneNumber)
    {
        if (string.IsNullOrWhiteSpace(rawPhoneNumber))
            return string.Empty;
        
        var digitsOnly = Regex.Replace(rawPhoneNumber, @"\D", "");
        
        if (!digitsOnly.StartsWith("55"))
            digitsOnly = "55" + digitsOnly;

        return digitsOnly;
    }
    
    private string GetStatusEmoji(ERepairStatus status) => status switch
    {
        ERepairStatus.Approved => "✅",
        ERepairStatus.Disapproved => "❌",
        _ => "⚠️"
    };
}

