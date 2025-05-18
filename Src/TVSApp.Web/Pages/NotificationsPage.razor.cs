using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using TVS_App.Domain.Entities;
using TVSApp.Web.Handlers;

namespace TVSApp.Web.Pages;

public partial class NotificationsPage : ComponentBase
{
    public List<Notification> Notifications { get; set; } = new();

    [Inject] public NotificationHandler NotificationHandler { get; set; } = null!;
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    [Inject] public HubConnection HubConnection { get; set; } = null!;
    [Inject] public IDialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            HubConnection.On<string>("Atualizar", async (mensagem) =>
            {
                Console.WriteLine($"Web socket Recebido: {mensagem}");
                await LoadNotifications();
            });

            if (HubConnection.State == HubConnectionState.Disconnected)
            {
                await HubConnection.StartAsync();
                Console.WriteLine("SignalR conectado");
            }
            
            await LoadNotifications();
        }
        catch (Exception e)
        {
            Snackbar.Add($"Erro: {e.Message}", Severity.Error);
        }
    }

    public async Task LoadNotifications()
    {
        try
        {
            var result = await NotificationHandler.GetUnreadNotifications();
            if (result.IsSuccess && result.Data != null)
            {
                Notifications = result.Data;
                StateHasChanged();
            }
            else
                Snackbar.Add($"Erro: {result.Message}", Severity.Error);
        }
        catch (Exception e)
        {
            Snackbar.Add($"Erro: {e.Message}", Severity.Error);
        }
    }
    
    private async Task MarkAsRead(long id)
    {
        try
        {
            var result = await NotificationHandler.MarkNotificationAsRead(id);
            if (result.IsSuccess)
            {
                Snackbar.Add("Notificação marcada como lida.", Severity.Success);
                await LoadNotifications();
            }
            else
            {
                Snackbar.Add($"Erro: {result.Message}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erro ao marcar como lida: {ex.Message}", Severity.Error);
        }
    }
}