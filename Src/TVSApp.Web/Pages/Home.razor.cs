using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using TVS_App.Application.Commands;
using TVS_App.Domain.Entities;
using TVSApp.Web.Components.Dialogs;
using TVSApp.Web.Handlers;

namespace TVSApp.Web.Pages;

public partial class Home : ComponentBase
{
    public List<ServiceOrder> PendingEstimatesServiceOrders { get; set; } = new();
    public List<ServiceOrder> WaitingResponseServiceOrders { get; set; } = new();
    public List<ServiceOrder> PendingPurchaseServiceOrders { get; set; } = new();
    public List<ServiceOrder> WaitingPartsServiceOrders { get; set; } = new();
    public List<ServiceOrder> WaitingPickupServiceOrders { get; set; } = new();
    public List<ServiceOrder> DeliveredServiceOrders { get; set; } = new();
    private bool _isBusy;

    [Inject] public ServiceOrderHandler Handler { get; set; } = null!;
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    [Inject] public HubConnection HubConnection { get; set; } = null!;
    [Inject] public IDialogService DialogService { get; set; } = null!;
    
    protected override async Task OnInitializedAsync()
    {
        try
        {
            await UpdateServiceOrdersAsync();
            
            HubConnection.On<string>("Atualizar", async (mensagem) =>
            {
                Console.WriteLine($"Web socket Recebido: {mensagem}");
                await UpdateServiceOrdersAsync();
            });

            if (HubConnection.State == HubConnectionState.Disconnected)
            {
                await HubConnection.StartAsync();
                Console.WriteLine("SignalR conectado");
            }
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
                    .Cast<ServiceOrder>()
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
                    .Cast<ServiceOrder>()
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
                    .Cast<ServiceOrder>()
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
                    .Cast<ServiceOrder>()
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
                    .Cast<ServiceOrder>()
                    .ToList();
            else
                Snackbar.Add(waitingPickupresult.Message ?? "Ocorreu um erro ao carregar  os serviços aguardando coleta.");
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
    
    public async Task LoadDeliveredAsync()
    {
        try
        {
            var deliveredresult = await Handler.GetDeliveredAsync(new PaginationCommand{PageNumber = 1, PageSize = 500});
            if (deliveredresult.IsSuccess && deliveredresult.Data != null)
                DeliveredServiceOrders = deliveredresult.Data.Items
                    .Where(item => item is not null)
                    .Cast<ServiceOrder>()
                    .ToList();
            else
                Snackbar.Add(deliveredresult.Message ?? "Ocorreu um erro ao carregar  os serviços entregues.");
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
            await LoadDeliveredAsync();
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
    
    private async Task OpenAddEstimateDialog(ServiceOrder order)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = order,
            ["OnEstimateAdded"] = EventCallback.Factory.Create(this, UpdateServiceOrdersAsync)
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.False,
            FullScreen = true
        };

        var dialog = await DialogService.ShowAsync<AddEstimateDialog>("Atualizar Ordem de Serviço", parameters, options);
        var result = await dialog.Result;
    
        if (result!.Canceled)
        {
            await UpdateServiceOrdersAsync();
        }
    }
    
    private async Task OpenAddResponseDialog(ServiceOrder order)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = order,
            ["OnResponseAdded"] = EventCallback.Factory.Create(this, UpdateServiceOrdersAsync)
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.False,
            FullScreen = true
        };

        var dialog = await DialogService.ShowAsync<AddResponseDialog>("Atualizar Ordem de Serviço", parameters, options);
        var result = await dialog.Result;
    
        if (result!.Canceled)
        {
            await UpdateServiceOrdersAsync();
        }
    }
    
    private async Task OpenAddPerchasePartDialog(ServiceOrder order)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = order,
            ["OnPurchasePartAdded"] = EventCallback.Factory.Create(this, UpdateServiceOrdersAsync)
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.False,
            FullScreen = true
        };

        var dialog = await DialogService.ShowAsync<AddPurchasePartDialog>("Atualizar Ordem de Serviço", parameters, options);
        var result = await dialog.Result;
    
        if (result!.Canceled)
        {
            await UpdateServiceOrdersAsync();
        }
    }
    
    private async Task OpenAddRepairDialog(ServiceOrder order)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = order,
            ["OnRepairAdded"] = EventCallback.Factory.Create(this, UpdateServiceOrdersAsync)
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.False,
            FullScreen = true
        };

        var dialog = await DialogService.ShowAsync<AddRepairDialog>("Atualizar Ordem de Serviço", parameters, options);
        var result = await dialog.Result;
    
        if (result!.Canceled)
        {
            await UpdateServiceOrdersAsync();
        }
    }
    
    private async Task OpenAddDeliveryDialog(ServiceOrder order)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = order,
            ["OnDeliveryAdded"] = EventCallback.Factory.Create(this, UpdateServiceOrdersAsync)
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.False,
            FullScreen = true
        };

        var dialog = await DialogService.ShowAsync<AddDeliveryDialog>("Atualizar Ordem de Serviço", parameters, options);
        var result = await dialog.Result;
    
        if (result!.Canceled)
        {
            await UpdateServiceOrdersAsync();
        }
    }
}