using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using TVS_App.Application.Commands.ServiceOrderCommands;
using TVS_App.Domain.Entities;
using TVSApp.Web.Components.Dialogs;
using TVSApp.Web.Handlers;

namespace TVSApp.Web.Pages;

public partial class EditServiceOrderPage : ComponentBase
{
    [Inject] public CustomerHandler CustomerHandler { get; set; } = null!;
    public Customer SelectedCustomer { get; set; } = new();
    private string _serviceOrderId = string.Empty;
    private GetServiceOrderByIdCommand _printCommand = new();
    private AddProductLocationCommand _locationCommand = new();
    private bool _isBusy;
    
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    [Inject] public IDialogService DialogService { get; set; } = null!;
    [Inject] public ServiceOrderHandler ServiceOrderHandler { get; set; } = null!;
    

    public async Task OpenEditServiceOrderDialog()
    {
        _isBusy = true;
        var normalizeNumber = new string(_serviceOrderId.Where(char.IsDigit).ToArray());
        if (!long.TryParse(normalizeNumber, out var longNumber) || longNumber <= 0)
        {
            Snackbar.Add("Número da OS inválido", Severity.Error);
            return;
        }
        
        var serviceOrderResult = await ServiceOrderHandler.GetServiceOrderById(
            new GetServiceOrderByIdCommand{Id = longNumber});
        if (serviceOrderResult.IsSuccess)
        {
            if (serviceOrderResult.Data != null)
            {
                await OpenEditDialog(serviceOrderResult.Data);
            }
            else
            {
                Snackbar.Add("Ocorreu um erro ao tentar buscar a ordem de serviço");
            }
        }
        else
        {
            Snackbar.Add(serviceOrderResult.Message ?? "Ocorreu um erro ao tentar buscar a ordem de serviço", Severity.Error);
        }
        _isBusy = false;
    }
    
    public async Task OpenEditDialog(ServiceOrder order)
    {
        _isBusy = true;
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

        await DialogService.ShowAsync<EditServiceOrderDialog>("Editar ordem de serviço", parameters, options);
        _isBusy = false;
    }
    
    private async Task<IEnumerable<Customer>> SearchCustomers(string? searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 3)
            return Enumerable.Empty<Customer>();

        try
        {
            var result = await CustomerHandler.GetCustomerByNameAsync(searchTerm);

            if (!result.IsSuccess || result.Data == null)
            {
                Snackbar.Add(result.Message ?? "Erro ao buscar clientes", Severity.Error);
                return Enumerable.Empty<Customer>();
            }

            var normalizedTerm = Normalize(searchTerm);

            return result.Data
                .Where(c => Normalize(c.Name.CustomerName).Contains(normalizedTerm))
                .Take(20);
        }
        catch (Exception e)
        {
            Snackbar.Add($"Erro: {e.Message}", Severity.Error);
            return Enumerable.Empty<Customer>();
        }
    }
    
    public async Task OnCustomerSelected()
    {
        if (SelectedCustomer.Id == 0)
        {
            Snackbar.Add("Nenhum cliente foi selecionado", Severity.Error);
            return;
        }
        
        await OpenConfirmCustomerDialog(SelectedCustomer.Id);
    }
    
    private static string Normalize(string text)
    {
        return string.Concat(text.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
            .ToLowerInvariant();
    }
    
    private async Task OpenConfirmCustomerDialog(long customerId)
    {
        _isBusy = true;
        var parameters = new DialogParameters
        {
            ["CustomerId"] = customerId
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.False,
            FullScreen = true
        };

        var dialog = await DialogService.ShowAsync<ConfirmCustomerDialog>("Confirmar cliente", parameters, options);
        var result = await dialog.Result;
        _isBusy = false;
    }

    public async Task SetProductLocation()
    {
        _isBusy = true;
        try
        {
            var result = await ServiceOrderHandler.AddProductLocation(_locationCommand);
            if (result.IsSuccess)
                Snackbar.Add(result.Message ?? "Prateleira adicionada com sucesso!", Severity.Success);
            else
                Snackbar.Add(result.Message ?? "Ocorreu um erro ao adicionar a prateleira", Severity.Error);
        }
        catch (Exception e)
        {
            Snackbar.Add($"Erro: {e.Message}", Severity.Error);
        }
        _isBusy = false;
    }
}