using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;
using TVS_App.Infrastructure.Configurations;
using TVS_App.Infrastructure.Extensions;

namespace TVS_App.Infrastructure.Services.Whatsapp;

public class WhatsappService : IWhatsappService
{
    private readonly HttpClient _httpClient;
    private readonly EvolutionApiSettings _settings;

    public WhatsappService(HttpClient httpClient, IOptions<EvolutionApiSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }
    
    public async Task<BaseResponse<string>> SendWelcomeMessage(string serviceOrder, string customerName, string phoneNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serviceOrder))
                return new BaseResponse<string>(null, 404, "Ordem de serviço vazia");
            
            if (string.IsNullOrWhiteSpace(customerName))
                return new BaseResponse<string>(null, 404, "Nome do cliente vazio");
            
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return new BaseResponse<string>(null, 404, "Número de telefone vazio");

            var text = $"Olá {customerName}, seja bem-vindo(a) à TVS Eletrônica! 👋😊\n\n" +
                       $"📄 Recebemos sua ordem de serviço nº {serviceOrder} com sucesso ✅.\n\n" +
                       $"🔍 Nossa equipe já está analisando seu aparelho 🛠️ e em breve entraremos em contato com mais informações.\n\n" +
                       $"🙏 Agradecemos a confiança!\n\n" +
                       $"Atenciosamente,\nEquipe TVS Eletrônica 📺🔧✨";

            var url = $"{_settings.BaseUrl}/message/sendText/{_settings.Instance}";
            
            var requestBody = new
            {
                number = FormatPhoneNumber(phoneNumber),
                text,
                delay = 0,
                linkPreview = false,
                mentionsEveryOne = false
            };
            
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", _settings.ApiKey);
            request.Content = content;
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new BaseResponse<string>(null, (int)response.StatusCode, $"Erro ao enviar mensagem: {error}");
            }

            return new BaseResponse<string>("Mensagem enviada com sucesso!", 200, "Mensagem enviada com sucesso!");
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

    public async Task<BaseResponse<string>> SendEstimateMessage(
        string serviceOrder, 
        string securityCode, 
        ERepairResult? result, 
        string customerName, 
        string? solution, 
        string phoneNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serviceOrder))
                return new BaseResponse<string>(null, 404, "Ordem de serviço vazia");
            
            if (string.IsNullOrWhiteSpace(securityCode))
                return new BaseResponse<string>(null, 404, "Código de segurança vazio");

            if (string.IsNullOrWhiteSpace(customerName))
                return new BaseResponse<string>(null, 404, "Nome do cliente vazio");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return new BaseResponse<string>(null, 404, "Número de telefone vazio");

            var formattedNumber = FormatPhoneNumber(phoneNumber);
            string text;

            if (result == ERepairResult.Unrepaired || result == ERepairResult.NoDefectFound)
            {
                text = $"Olá {customerName}, tudo bem? 👋\n\n" +
                       $"Sobre a ordem de serviço nº {serviceOrder}, informamos:\n\n" +
                       $"🛠️ Situação: {result.GetDisplayName()}.\n" +
                       $"🔎 Detalhes: {solution}\n\n" +
                       $"Em caso de dúvidas, estamos à disposição.\n\n" +
                       $"Atenciosamente,\nTVS Eletrônica 📺🔧";
            }
            else
            {
                var link = $"https://www.tvseletronica.com/consultar?id={serviceOrder}&code={securityCode}";
                text = $"*Olá {customerName}, tudo bem? 👋*\n\n" +
                       $"*O orçamento referente à sua Ordem de Serviço nº {serviceOrder} está disponível no link abaixo:*\n\n" +
                       $"{link}\n\n" +
                       $"*Nele, você pode visualizar os detalhes e aprovar ou recusar o orçamento diretamente.*\n\n" +
                       $"Atenciosamente,\nTVS Eletrônica 📺🔧";
            }

            var urlEstimateText = $"{_settings.BaseUrl}/message/sendText/{_settings.Instance}";
            var requestBodyEstimateText = new
            {
                number = formattedNumber,
                text,
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
                $"Mensagem de orçamento enviada e enquete criada com sucesso!",
                200,
                "Mensagem e enquete enviadas com sucesso!");
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
    
    public async Task<BaseResponse<string>> SendDeviceReadyMessage(string serviceOrder, string customerName, string phoneNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serviceOrder))
                return new BaseResponse<string>(null, 404, "Ordem de serviço vazia");
            
            if (string.IsNullOrWhiteSpace(customerName))
                return new BaseResponse<string>(null, 404, "Nome do cliente vazio");
            
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return new BaseResponse<string>(null, 404, "Número de telefone vazio");

            var text = $"Olá {customerName}, tudo bem? 👋😊\n\n" +
                       $"📦 Sua ordem de serviço nº {serviceOrder} está pronta para retirada! 🎉✅\n" +
                       $"🧪 O seu aparelho já passou por todos os testes e consertos necessários 🛠️🔍.\n\n" +
                       $"🙏 Agradecemos por confiar na TVS Eletrônica. Estamos à disposição para qualquer dúvida! 💬\n\n" +
                       $"Atenciosamente,\nEquipe TVS Eletrônica 📺🔧✨";

            var url = $"{_settings.BaseUrl}/message/sendText/{_settings.Instance}";
            
            var requestBody = new
            {
                number = FormatPhoneNumber(phoneNumber),
                text,
                delay = 0,
                linkPreview = false,
                mentionsEveryOne = false
            };
            
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", _settings.ApiKey);
            request.Content = content;
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new BaseResponse<string>(null, (int)response.StatusCode, $"Erro ao enviar mensagem: {error}");
            }

            return new BaseResponse<string>("Mensagem enviada com sucesso!", 200, "Mensagem enviada com sucesso!");
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

    public async Task<BaseResponse<string>> SendGuaranteeMessage(string serviceOrder, string customerName, string guarantee, string phoneNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serviceOrder))
                return new BaseResponse<string>(null, 404, "Ordem de serviço vazia");
            
            if (string.IsNullOrWhiteSpace(customerName))
                return new BaseResponse<string>(null, 404, "Nome do cliente vazio");
            
            if (string.IsNullOrWhiteSpace(guarantee))
                return new BaseResponse<string>(null, 404, "Garantia vazia");
            
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return new BaseResponse<string>(null, 404, "Número de telefone vazio");

            var text = $"""
                        Olá {customerName}, tudo bem? 👋😊

                        Aqui é da TVS Eletrônica 📺🔧. Informamos que sua ordem de serviço nº {serviceOrder} foi finalizada ✅ e o produto já foi entregue 📬.

                        🛡️ Garantia: {guarantee}

                        🙏 Agradecemos pela confiança em nossos serviços! Qualquer dúvida ou necessidade, estamos à disposição 💬.

                        Atenciosamente,  
                        Equipe TVS Eletrônica ✨
                        """;

            var url = $"{_settings.BaseUrl}/message/sendText/{_settings.Instance}";
            
            var requestBody = new
            {
                number = FormatPhoneNumber(phoneNumber),
                text,
                delay = 0,
                linkPreview = false,
                mentionsEveryOne = false
            };
            
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", _settings.ApiKey);
            request.Content = content;
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new BaseResponse<string>(null, (int)response.StatusCode, $"Erro ao enviar mensagem: {error}");
            }

            return new BaseResponse<string>("Mensagem enviada com sucesso!", 200, "Mensagem enviada com sucesso!");
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

    public async Task<BaseResponse<string>> SendProductSaleMessage(string customerName, string phoneNumber, decimal amount,
        string guarantee, EProduct productType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customerName))
                return new BaseResponse<string>(null, 404, "Nome do cliente vazio");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return new BaseResponse<string>(null, 404, "Número de telefone vazio");

            if (string.IsNullOrWhiteSpace(guarantee))
                return new BaseResponse<string>(null, 404, "Garantia vazia");

            var formattedNumber = FormatPhoneNumber(phoneNumber);

            var formattedAmount = amount.ToString("C", new CultureInfo("pt-BR"));

            var text = $"Olá {customerName}, tudo bem? 👋\n\n" +
                       $"Agradecemos por adquirir um produto da TVS Eletrônica! 🛒\n\n" +
                       $"📦 Produto: {productType.ToString()}\n" +
                       $"💰 Valor: {formattedAmount}\n" +
                       $"🛡️ Garantia: {guarantee}\n\n" +
                       $"Em caso de dúvidas ou necessidade de suporte, estamos à disposição!\n\n" +
                       $"Atenciosamente,\nEquipe TVS Eletrônica 📺🔧";

            var url = $"{_settings.BaseUrl}/message/sendText/{_settings.Instance}";

            var requestBody = new
            {
                number = formattedNumber,
                text,
                delay = 0,
                linkPreview = false,
                mentionsEveryOne = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", _settings.ApiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new BaseResponse<string>(null, (int)response.StatusCode, $"Erro ao enviar mensagem: {error}");
            }

            return new BaseResponse<string>("Mensagem de venda enviada com sucesso!", 200, "Mensagem enviada com sucesso!");
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

    public async Task<BaseResponse<string>> SendRejectionReadyForPickupMessage(string customerName, string phoneNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customerName))
                return new BaseResponse<string>(null, 404, "Nome do cliente vazio");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return new BaseResponse<string>(null, 404, "Número de telefone vazio");

            var formattedNumber = FormatPhoneNumber(phoneNumber);

            var text = $"🚀 *Pronto para retirada, {customerName}!*\n\n" +
                       $"Seu equipamento está disponível na TVS Eletrônica:\n" +
                       $"🕒 09h-12h 13-18h (Seg-Sex)\n\n" +
                       $"⚠️ *Lembrete:* Orçamento não foi aprovado\n\n" +
                       $"📞 Dúvidas? Chame aqui mesmo!";
            
            var url = $"{_settings.BaseUrl}/message/sendText/{_settings.Instance}";

            var requestBody = new
            {
                number = formattedNumber,
                text,
                delay = 0,
                linkPreview = false,
                mentionsEveryOne = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", _settings.ApiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new BaseResponse<string>(null, (int)response.StatusCode, $"Erro ao enviar mensagem: {error}");
            }

            return new BaseResponse<string>("Mensagem de venda enviada com sucesso!", 200, "Mensagem enviada com sucesso!");
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

    public async Task<BaseResponse<string>> NotifyOwnerEstimateDecision(
        string serviceOrder, 
        ERepairStatus result, 
        string customerName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customerName))
                return new BaseResponse<string>(null, 404, "Nome do cliente vazio");

            var formattedNumber = "5541992744920";
            
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
            
            var url = $"{_settings.BaseUrl}/message/sendText/{_settings.Instance}";

            var requestBody = new
            {
                number = formattedNumber,
                text,
                delay = 0,
                linkPreview = false,
                mentionsEveryOne = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", _settings.ApiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new BaseResponse<string>(null, (int)response.StatusCode, $"Erro ao enviar mensagem: {error}");
            }

            return new BaseResponse<string>("Mensagem de venda enviada com sucesso!", 200, "Mensagem enviada com sucesso!");
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

    public async Task<BaseResponse<string>> SendThankYouForEstimateApprovalAsync(string serviceOrder, string customerName, string phoneNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customerName))
                return new BaseResponse<string>(null, 404, "Nome do cliente vazio");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return new BaseResponse<string>(null, 404, "Número de telefone vazio");

            var formattedNumber = FormatPhoneNumber(phoneNumber);

            var text = $"✨ *{customerName}, obrigado por confiar em nós!* ✨\n\n" +
                       $"🛠️ OS #{serviceOrder} em andamento\n" +
                       $"✅ Orçamento aprovado - reparo iniciado\n\n" +
                       $"📲 Avisaremos por aqui quando estiver pronto\n" +
                       $"📞 Dúvidas? Chame aqui mesmo!\n\n" +
                       $"🙌 *Equipe TVS Eletrônica*";


            var url = $"{_settings.BaseUrl}/message/sendText/{_settings.Instance}";

            var requestBody = new
            {
                number = formattedNumber,
                text,
                delay = 0,
                linkPreview = false,
                mentionsEveryOne = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", _settings.ApiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new BaseResponse<string>(null, (int)response.StatusCode, $"Erro ao enviar mensagem: {error}");
            }

            return new BaseResponse<string>("Mensagem de venda enviada com sucesso!", 200, "Mensagem enviada com sucesso!");
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

    public async Task<BaseResponse<string>> SendEstimateDisapprovalAcknowledgmentAsync(string serviceOrder, string customerName, string phoneNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customerName))
                return new BaseResponse<string>(null, 404, "Nome do cliente vazio");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return new BaseResponse<string>(null, 404, "Número de telefone vazio");

            var formattedNumber = FormatPhoneNumber(phoneNumber);

            var text = $"✨ *{customerName}*, recebemos sua resposta! ✨\n\n" +
                       $"🔹 OS: #{serviceOrder}\n" +
                       $"🔹 Status: Orçamento não aprovado\n\n" +
                       $"⏳ *Avisaremos pelo WhatsApp* quando seu equipamento estiver pronto para retirada (em até 2 dias úteis)*\n" +
                       $"📞 Dúvidas? TVS Eletrônica à disposição!\n\n" +
                       $"💙 Agradecemos pela confiança!";
            
            var url = $"{_settings.BaseUrl}/message/sendText/{_settings.Instance}";

            var requestBody = new
            {
                number = formattedNumber,
                text,
                delay = 0,
                linkPreview = false,
                mentionsEveryOne = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", _settings.ApiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new BaseResponse<string>(null, (int)response.StatusCode, $"Erro ao enviar mensagem: {error}");
            }

            return new BaseResponse<string>("Mensagem de venda enviada com sucesso!", 200, "Mensagem enviada com sucesso!");
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

