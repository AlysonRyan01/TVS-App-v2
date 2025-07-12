using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using TVS_App.Application.DTOs;
using TVS_App.Application.Requests.CustomerRequests.CustomerCommands;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVSApp.Web.Components.Dialogs;
using TVSApp.Web.Handlers;
using TVSApp.Web.Models.Customers;
using TVSApp.Web.Models.ServiceOrders;

namespace TVSApp.Web.Pages;

public partial class CreateServiceOrderPage : ComponentBase
{
    public CreateCustomerCommandModel CreateCustomerCommand { get; set; } = new();
    public CreateServiceOrderCommandModel CreateServiceOrderCommand { get; set; } = new();
    public CustomerDto? SelectedCustomer { get; set; }
    private bool _isLoading;
    private bool _customerSideCompleted;
    private bool _serviceOrderTime;

    private CreateServiceOrderCommand ModelToCommand()
    {
        return new CreateServiceOrderCommand(
            CreateServiceOrderCommand.CustomerId,
            CreateServiceOrderCommand.ProductBrand,
            CreateServiceOrderCommand.ProductModel,
            CreateServiceOrderCommand.ProductSerialNumber,
            CreateServiceOrderCommand.ProductDefect,
            CreateServiceOrderCommand.Accessories,
            CreateServiceOrderCommand.ProductType,
            CreateServiceOrderCommand.Enterprise);
    }

    [Inject] public CustomerHandler CustomerHandler { get; set; } = null!;
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    [Inject] public IDialogService DialogService { get; set; } = null!;

    private async Task<IEnumerable<CustomerDto>> SearchCustomers(string? searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<CustomerDto>();

        try
        {
            _isLoading = true;
            var result = await CustomerHandler.GetCustomerByNameAsync(searchTerm);

            if (!result.IsSuccess || result.Data == null)
            {
                Snackbar.Add(result.Message ?? "Erro ao buscar clientes", Severity.Error);
                return Enumerable.Empty<CustomerDto>();
            }

            var normalizedTerm = Normalize(searchTerm);
            
            var customers = result.Data;

            return customers
                .Where(c => Normalize(c.Name).Contains(normalizedTerm))
                .Take(20);
        }
        catch (Exception e)
        {
            Snackbar.Add($"Erro: {e.Message}", Severity.Error);
            return Enumerable.Empty<CustomerDto>();
        }
        finally
        {
            _isLoading = false;
        }
    }
    
    public async Task OnCustomerSelected()
    {
        if (SelectedCustomer == null || SelectedCustomer.Id == 0)
        {
            Snackbar.Add("Nenhum cliente foi selecionado", Severity.Error);
            return;
        }

        await OpenConfirmCustomerDialog(SelectedCustomer.Id);
    }

    public async Task CreateCustomer()
    {
        _isLoading = true;
        try
        {
            var command = new CreateCustomerCommand(
                CreateCustomerCommand.Name,
                CreateCustomerCommand.Street,
                CreateCustomerCommand.Neighborhood,
                CreateCustomerCommand.City,
                CreateCustomerCommand.Number,
                CreateCustomerCommand.ZipCode,
                CreateCustomerCommand.State,
                CreateCustomerCommand.Phone,
                CreateCustomerCommand.Phone2,
                CreateCustomerCommand.Email,
                CreateCustomerCommand.Cpf);
            
            var createResult = await CustomerHandler.CreateCustomerAsync(command);
            if (createResult.IsSuccess || createResult.Data != null)
            {
                SelectedCustomer = createResult.Data!;
                _customerSideCompleted = true;
                _serviceOrderTime = true;
                CreateServiceOrderCommand.CustomerId = createResult.Data!.Id;
                Snackbar.Add("Cliente criado com sucesso!", Severity.Success);
            }
            else
            {
                Snackbar.Add($"Erro: {createResult.Message}", Severity.Error);
            }
        }
        catch (Exception e)
        {
            Snackbar.Add(e.Message, Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    public void CancelOrder()
    {
        _customerSideCompleted = false;
        _serviceOrderTime = false;
    }
    
    private static string Normalize(string text)
    {
        return string.Concat(text.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
            .ToLowerInvariant();
    }
    
    private async Task OpenConfirmCustomerDialog(long customerId)
    {
        var parameters = new DialogParameters
        {
            ["CustomerId"] = customerId
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium
        };

        var dialog = await DialogService.ShowAsync<ConfirmCustomerDialog>("Confirmar cliente", parameters, options);
        var result = await dialog.Result;
    
        if (!result!.Canceled)
        {
            var confirmedCustomer = (CustomerDto)result.Data!;
        
            if (SelectedCustomer != null)
            {
                SelectedCustomer = confirmedCustomer;
                CreateServiceOrderCommand.CustomerId = SelectedCustomer.Id;
                _serviceOrderTime = true;
                _customerSideCompleted = true;
            }
            else
            {
                Snackbar.Add("Ocorreu um erro ao confirmar o cliente", Severity.Error);
            }
        }
    }
}