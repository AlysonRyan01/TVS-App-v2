using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using TVS_App.Application.Commands;
using TVS_App.Application.Commands.ServiceOrderCommands;
using TVS_App.Application.DTOs;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Domain.Entities;
using TVSApp.Web.Components.Dialogs;
using TVSApp.Web.Handlers;

namespace TVSApp.Web.Pages;

public partial class SearchPage : ComponentBase
{
    public List<ServiceOrderDto> FilteredServiceOrders { get; set; } = new();
    private CustomerDto? _customerFilter;
    private string _numberFilter = String.Empty;
    private string _serialNumberFilter = String.Empty;
    private string _modelFilter = String.Empty;
    private DateTime? _startDateFilter;
    private DateTime? _endDateFilter;
    private bool _isLoading;
    
    [Inject] public CustomerHandler CustomerHandler { get; set; } = null!;
    [Inject] public ServiceOrderHandler ServiceOrderHandler { get; set; } = null!;
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    [Inject] public IDialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var allOrdersResult =
                await ServiceOrderHandler.GetAllServiceOrdersAsync(new PaginationCommand { PageNumber = 1, PageSize = 300 });
            if (allOrdersResult.IsSuccess && allOrdersResult.Data?.Items.Any() == true)
            {
                FilteredServiceOrders = allOrdersResult.Data.Items
                    .Where(x => x is not null)
                    .Select(x => x!)
                    .ToList();
            }
        }
        catch (Exception e)
        {
            Snackbar.Add($"Erro: {e.Message}", Severity.Error);
        }
    }

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

    public async Task SearchByCustomerNameAsync()
    {
        try
        {
            FilteredServiceOrders = new();
            
            if (_customerFilter == null || _customerFilter.Id == 0)
            {
                Snackbar.Add("Voce precisa digitar o nome de algum cliente", Severity.Error);
                return;
            }
               
            var searchResult = await ServiceOrderHandler.
                GetServiceOrdersByCustomerName(_customerFilter.Name);

            if (searchResult.IsSuccess)
            {
                FilteredServiceOrders = searchResult.Data!;
                StateHasChanged();
                Snackbar.Add("Ordens de serviço filtradas com sucesso!", Severity.Success);
            }
            else
            {
                Snackbar.Add($"Erro: {searchResult.Message}", Severity.Error);
            }
        }
        catch (Exception e)
        {
            Snackbar.Add($"Erro: {e.Message}", Severity.Error);
        }
    }
    
    public async Task SearchByNumberAsync()
    {
        try
        {
            FilteredServiceOrders = new();
            
            if (string.IsNullOrEmpty(_numberFilter))
            {
                Snackbar.Add("Voce precisa digitar o número da ordem", Severity.Error);
                return;
            }
            
            var normalizeNumber = new string(_numberFilter.Where(char.IsDigit).ToArray());
            if (!long.TryParse(normalizeNumber, out var longNumber) || longNumber <= 0)
            {
                Snackbar.Add("Número da OS inválido", Severity.Error);
                return;
            }
               
            var searchResult = await ServiceOrderHandler.
                GetServiceOrderById(new GetServiceOrderByIdCommand { Id = longNumber });

            if (searchResult.IsSuccess)
            {
                if (searchResult.Data!.Id != 0)
                {
                    FilteredServiceOrders.Add(searchResult.Data!);
                    StateHasChanged();
                    Snackbar.Add("Ordens de serviço filtradas com sucesso!", Severity.Success);
                }
                else
                {
                    Snackbar.Add($"Nenhuma ordem de serviço foi encontrada com o número: {longNumber}", Severity.Error);
                }
            }
            else
            {
                Snackbar.Add($"Erro: {searchResult.Message}", Severity.Error);
            }
        }
        catch (Exception e)
        {
            Snackbar.Add($"Erro: {e.Message}", Severity.Error);
        }
    }
    
    public async Task SearchBySerialNumberAsync()
    {
        try
        {
            FilteredServiceOrders = new();
            
            if (string.IsNullOrEmpty(_serialNumberFilter))
            {
                Snackbar.Add("Voce precisa digitar o número de série", Severity.Error);
                return;
            }
               
            var searchResult = await ServiceOrderHandler.
                GetServiceOrdersBySerialNumber(_serialNumberFilter);

            if (searchResult.IsSuccess)
            {
                FilteredServiceOrders = searchResult.Data!;
                StateHasChanged();
                Snackbar.Add(searchResult.Message ?? "Ordens de serviço filtradas com sucesso!", Severity.Success);
            }
            else
            {
                Snackbar.Add($"Erro: {searchResult.Message}", Severity.Error);
            }
        }
        catch (Exception e)
        {
            Snackbar.Add($"Erro: {e.Message}", Severity.Error);
        }
    }
    
    public async Task SearchByModelAsync()
    {
        try
        {
            FilteredServiceOrders = new();
            
            if (string.IsNullOrEmpty(_modelFilter))
            {
                Snackbar.Add("Voce precisa digitar o modelo do produto", Severity.Error);
                return;
            }
               
            var searchResult = await ServiceOrderHandler.
                GetServiceOrdersByModel(_modelFilter);

            if (searchResult.IsSuccess)
            {
                FilteredServiceOrders = searchResult.Data!;
                StateHasChanged();
                Snackbar.Add(searchResult.Message ?? "Ordens de serviço filtradas com sucesso!", Severity.Success);
            }
            else
            {
                Snackbar.Add($"Erro: {searchResult.Message}", Severity.Error);
            }
        }
        catch (Exception e)
        {
            Snackbar.Add($"Erro: {e.Message}", Severity.Error);
        }
    }
    
    public async Task SearchByDateAsync()
    {
        try
        {
            DateTime nonNullableStartDate;
            DateTime nonNullableEndDate;
            
            FilteredServiceOrders = new();

            if (_startDateFilter == null || _endDateFilter == null)
            {
                Snackbar.Add("Voce precisa definir as datas", Severity.Error);
                return;
            }
            
            nonNullableStartDate = _startDateFilter.Value;
            nonNullableEndDate = _endDateFilter.Value;
               
            var searchResult = await ServiceOrderHandler.
                GetServiceOrdersByDate(nonNullableStartDate,  nonNullableEndDate);

            if (searchResult.IsSuccess)
            {
                FilteredServiceOrders = searchResult.Data!;
                StateHasChanged();
                Snackbar.Add(searchResult.Message ?? "Ordens de serviço filtradas com sucesso!", Severity.Success);
            }
            else
            {
                Snackbar.Add($"Erro: {searchResult.Message}", Severity.Error);
            }
        }
        catch (Exception e)
        {
            Snackbar.Add($"Erro: {e.Message}", Severity.Error);
        }
    }

    public async Task OnSelectOrder(ServiceOrderDto order)
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

        await DialogService.ShowAsync<InspectServiceOrderDialog>("Inspecionar ordem de serviço", parameters, options);
    }
    
    private static string Normalize(string text)
    {
        return string.Concat(text.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
            .ToLowerInvariant();
    }
}