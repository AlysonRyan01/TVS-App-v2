using System.Net.Http.Json;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVSApp.Web.Handlers;

public class NotificationHandler
{
    private readonly HttpClient _httpClient;
    
    public NotificationHandler(IHttpClientFactory clientFactory)
    {
        _httpClient = clientFactory.CreateClient("api");
    }
    
    public async Task<BaseResponse<Notification>> CreateNotification(string title, string message)
    {
        try
        {
            var content = new
            {
                title,
                message
            };

            var response = await _httpClient.PostAsJsonAsync("/notifications", content);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<Notification>(null, (int)response.StatusCode, 
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }

            var contentEndpoint = await response.Content.ReadFromJsonAsync<BaseResponse<Notification>>();
            if (contentEndpoint == null || !contentEndpoint.IsSuccess)
                return new BaseResponse<Notification>(null, 500, contentEndpoint?.Message);

            return contentEndpoint;
        }
        catch
        {
            return new BaseResponse<Notification>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<List<Notification>>> GetUnreadNotifications()
    {
        try
        {
            var response =
                await _httpClient.GetFromJsonAsync<BaseResponse<List<Notification>>>("/notifications/unread");
            if (response == null || !response.IsSuccess)
                return new BaseResponse<List<Notification>>(null, 500, response?.Message ?? "Erro na API");

            return response;
        }
        catch
        {
            return new BaseResponse<List<Notification>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<Notification>> MarkNotificationAsRead(long id)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/notifications/{id}/read", id);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<Notification>(null, (int)response.StatusCode, 
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }
            
            var content = await response.Content.ReadFromJsonAsync<BaseResponse<Notification>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<Notification>(null, 500, content?.Message);
            
            return content;
        }
        catch
        {
            return new BaseResponse<Notification>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
}