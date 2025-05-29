using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using TVS_App.Application.Commands;
using TVS_App.Application.DTOs.ServiceOrder;
using TVSApp.Web.Components.Dialogs;
using TVSApp.Web.Handlers;

namespace TVSApp.Web.Pages;

public partial class Home : ComponentBase
{
    public List<ServiceOrderDto> PendingEstimatesServiceOrders { get; set; } = new();
    public List<ServiceOrderDto> WaitingResponseServiceOrders { get; set; } = new();
    public List<ServiceOrderDto> PendingPurchaseServiceOrders { get; set; } = new();
    public List<ServiceOrderDto> WaitingPartsServiceOrders { get; set; } = new();
    public List<ServiceOrderDto> WaitingPickupServiceOrders { get; set; } = new();
    private bool _isBusy;
    private bool _hubHandlerRegistered;

    [Inject] public ServiceOrderHandler Handler { get; set; } = null!;
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    [Inject] public HubConnection HubConnection { get; set; } = null!;
    [Inject] public IDialogService DialogService { get; set; } = null!;
    
    protected override async Task OnInitializedAsync()
    {
        try
        {
            await UpdateServiceOrdersAsync();
            RegisterHandlers();
        
            await TryStartConnection();
        
            HubConnection.Closed += HandleConnectionClosed;
            HubConnection.Reconnected += HandleReconnected;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    public async Task LoadPendingEstimatesAsync()
    {
        try
        {
            var pendingEstimatesresult = await Handler.GetPendingEstimatesAsync(new PaginationCommand{PageNumber = 1, PageSize = 500});
            if (pendingEstimatesresult.IsSuccess && pendingEstimatesresult.Data != null)
                PendingEstimatesServiceOrders = pendingEstimatesresult.Data.Items
                    .Where(item => item is not null)
                    .Cast<ServiceOrderDto>()
                    .ToList();
            else
                Snackbar.Add(pendingEstimatesresult.Message ?? "Ocorreu um erro ao carregar os orçamementos pendentes.");
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    public async Task LoadWaitingResponseAsync()
    {
        try
        {
            var waitingResponseresult = await Handler.GetWaitingResponseAsync(new PaginationCommand{PageNumber = 1, PageSize = 500});
            if (waitingResponseresult.IsSuccess && waitingResponseresult.Data != null)
                WaitingResponseServiceOrders = waitingResponseresult.Data.Items
                    .Where(item => item is not null)
                    .Cast<ServiceOrderDto>()
                    .ToList();
            else
                Snackbar.Add(waitingResponseresult.Message ?? "Ocorreu um erro ao carregar  os serviços com respostas pendentes.");
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
    
    public async Task LoadPendingPurchaseAsync()
    {
        try
        {
            var pendingPurchaseresult = await Handler.GetPendingPurchasePartAsync(new PaginationCommand{PageNumber = 1, PageSize = 500});
            if (pendingPurchaseresult.IsSuccess && pendingPurchaseresult.Data != null)
                PendingPurchaseServiceOrders = pendingPurchaseresult.Data.Items
                    .Where(item => item is not null)
                    .Cast<ServiceOrderDto>()
                    .ToList();
            else
                Snackbar.Add(pendingPurchaseresult.Message ?? "Ocorreu um erro ao carregar  os serviços com compra de peças pendentes.");
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
    
    public async Task LoadWaitingPartsAsync()
    {
        try
        {
            var waitingPartsresult = await Handler.GetWaitingPartsAsync(new PaginationCommand{PageNumber = 1, PageSize = 500});
            if (waitingPartsresult.IsSuccess && waitingPartsresult.Data != null)
                WaitingPartsServiceOrders = waitingPartsresult.Data.Items
                    .Where(item => item is not null)
                    .Cast<ServiceOrderDto>()
                    .ToList();
            else
                Snackbar.Add(waitingPartsresult.Message ?? "Ocorreu um erro ao carregar  os serviços entrega de peças pendentes.");
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
    
    public async Task LoadWaitingPickupAsync()
    {
        try
        {
            var waitingPickupresult = await Handler.GetWaitingPickupAsync(new PaginationCommand{PageNumber = 1, PageSize = 500});
            if (waitingPickupresult.IsSuccess && waitingPickupresult.Data != null)
                WaitingPickupServiceOrders = waitingPickupresult.Data.Items
                    .Where(item => item is not null)
                    .Cast<ServiceOrderDto>()
                    .ToList();
            else
                Snackbar.Add(waitingPickupresult.Message ?? "Ocorreu um erro ao carregar  os serviços aguardando coleta.");
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
    
    public async Task UpdateServiceOrdersAsync()
    {
        _isBusy = true;
        StateHasChanged();
        try
        {
            await LoadPendingEstimatesAsync();
            await LoadWaitingResponseAsync();
            await LoadPendingPurchaseAsync();
            await LoadWaitingPartsAsync();
            await LoadWaitingPickupAsync();
            StateHasChanged();
        }
        catch (Exception e)
        {
            Snackbar.Add(e.Message, Severity.Error);
        }
        finally
        {
            _isBusy = false;
            StateHasChanged();
        }
    }
    
    private async Task OpenAddEstimateDialog(ServiceOrderDto order)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = order
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.False,
            FullScreen = true
        };

        var dialog = await DialogService.ShowAsync<AddEstimateDialog>("Atualizar Ordem de Serviço", parameters, options);
        await dialog.Result;
    }
    
    private async Task OpenAddResponseDialog(ServiceOrderDto order)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = order
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.False,
            FullScreen = true
        };

        var dialog = await DialogService.ShowAsync<AddResponseDialog>("Atualizar Ordem de Serviço", parameters, options);
        await dialog.Result;
    }
    
    private async Task OpenAddPerchasePartDialog(ServiceOrderDto order)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = order
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.False,
            FullScreen = true
        };

        var dialog = await DialogService.ShowAsync<AddPurchasePartDialog>("Atualizar Ordem de Serviço", parameters, options);
        await dialog.Result;
    }
    
    private async Task OpenAddRepairDialog(ServiceOrderDto order)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = order
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.False,
            FullScreen = true
        };

        var dialog = await DialogService.ShowAsync<AddRepairDialog>("Atualizar Ordem de Serviço", parameters, options);
        await dialog.Result;
    }
    
    private async Task OpenAddDeliveryDialog(ServiceOrderDto order)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = order
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.False,
            FullScreen = true
        };

        var dialog = await DialogService.ShowAsync<AddDeliveryDialog>("Atualizar Ordem de Serviço", parameters, options);
        await dialog.Result;
    }
    
    private async Task TryStartConnection()
    {
        int maxAttempts = 5;
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                if (HubConnection.State == HubConnectionState.Disconnected)
                {
                    await HubConnection.StartAsync();
                    Console.WriteLine("SignalR conectado");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tentativa {i + 1} de conexão falhou: {ex.Message}");
                if (i == maxAttempts - 1)
                    throw;
            
                await Task.Delay(CalculateRetryDelay(i));
            }
        }
    }

    private TimeSpan CalculateRetryDelay(int attempt)
    {
        double delay = Math.Min(Math.Pow(2, attempt) * 500, 10000);
        return TimeSpan.FromMilliseconds(delay);
    }
    
    private void RegisterHandlers()
    {
        if (_hubHandlerRegistered) return;
    
        HubConnection.On<string>("Atualizar", async (mensagem) =>
        {
            Console.WriteLine($"Web socket Recebido: {mensagem}");
            await InvokeAsync(StateHasChanged);
            await UpdateServiceOrdersAsync();
        });
    
        _hubHandlerRegistered = true;
    }
    
    private async Task HandleConnectionClosed(Exception? error)
    {
        Console.WriteLine($"Conexão perdida: {error?.Message}");
        await TryStartConnection();
    }

    private async Task HandleReconnected(string? connectionId)
    {
        Console.WriteLine($"Reconectado com ID: {connectionId}");
        await UpdateServiceOrdersAsync();
    }
}