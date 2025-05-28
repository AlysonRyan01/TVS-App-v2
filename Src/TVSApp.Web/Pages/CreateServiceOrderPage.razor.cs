using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using TVS_App.Application.Commands.CustomerCommands;
using TVS_App.Application.Commands.ServiceOrderCommands;
using TVS_App.Application.DTOs;
using TVS_App.Application.Exceptions;
using TVSApp.Web.Components.Dialogs;
using TVSApp.Web.Handlers;

namespace TVSApp.Web.Pages;

public partial class CreateServiceOrderPage : ComponentBase
{
    public CreateCustomerCommand CreateCustomerCommand { get; set; } = new();
    public CreateServiceOrderCommand CreateServiceOrderCommand { get; set; } = new();
    public CustomerDto? SelectedCustomer { get; set; }
    private bool _isLoading;
    private bool _customerSideCompleted;
    private bool _serviceOrderTime;

    [Inject] public CustomerHandler CustomerHandler { get; set; } = null!;
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    [Inject] public IDialogService DialogService { get; set; } = null!;

    private async Task<IEnumerable<CustomerDto>> SearchCustomers(string? searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 3)
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
            CreateCustomerCommand.Normalize();
            CreateCustomerCommand.Validate();

            var createResult = await CustomerHandler.CreateCustomerAsync(CreateCustomerCommand);
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
        catch (CommandException<CreateCustomerCommand> ex)
        {
            Snackbar.Add($"Validação: {ex.Message}", Severity.Warning);
        }
        catch (Exception e)
        {
            Snackbar.Add($"Erro: {e.Message}", Severity.Error);
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