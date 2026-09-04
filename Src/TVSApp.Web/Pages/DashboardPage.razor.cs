using Microsoft.AspNetCore.Components;
using MudBlazor;
using TVS_App.Application.DTOs.Dashboard;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVSApp.Web.Handlers;

namespace TVSApp.Web.Pages;

public partial class DashboardPage : ComponentBase
{
    [Inject] public DashboardHandler Handler { get; set; } = null!;
    [Inject] public ISnackbar Snackbar { get; set; } = null!;

    private DashboardMetricsDto? _metrics;
    private bool _isBusy;

    private int _selectedMonth = DateTime.Now.Month;
    private int _selectedYear = DateTime.Now.Year;

    private DateTime? _rangeStartDate;
    private DateTime? _rangeEndDate;

    private bool _isRangeMode => _rangeStartDate.HasValue && _rangeEndDate.HasValue;

    private static readonly List<(int Value, string Label)> Months =
    [
        (1, "Janeiro"), (2, "Fevereiro"), (3, "Março"), (4, "Abril"),
        (5, "Maio"), (6, "Junho"), (7, "Julho"), (8, "Agosto"),
        (9, "Setembro"), (10, "Outubro"), (11, "Novembro"), (12, "Dezembro")
    ];

    private static List<int> Years =>
        Enumerable.Range(DateTime.Now.Year - 3, 4).Reverse().ToList();

    private decimal TotalRevenueCombined => _metrics!.TotalRevenue + _metrics.TotalSalesRevenue;
    private decimal NetTotal => TotalRevenueCombined - _metrics!.TotalPartsSpend;

    private string PeriodLabel => _isRangeMode
        ? $"{_rangeStartDate!.Value:dd/MM/yyyy} até {_rangeEndDate!.Value:dd/MM/yyyy}"
        : $"{Months.First(m => m.Value == _selectedMonth).Label}/{_selectedYear}";

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _isBusy = true;
        StateHasChanged();

        var result = await Handler.GetDashboardMetricsAsync(_selectedMonth, _selectedYear);

        if (result.IsSuccess && result.Data != null)
            _metrics = result.Data;
        else
            Snackbar.Add(result.Message ?? "Erro ao carregar métricas.", Severity.Error);

        _isBusy = false;
        StateHasChanged();
    }

    private async Task OnMonthChanged(int month)
    {
        _selectedMonth = month;
        _rangeStartDate = null;
        _rangeEndDate = null;
        await LoadAsync();
    }

    private async Task OnYearChanged(int year)
    {
        _selectedYear = year;
        _rangeStartDate = null;
        _rangeEndDate = null;
        await LoadAsync();
    }

    private async Task LoadByRangeAsync()
    {
        if (_rangeStartDate is null || _rangeEndDate is null)
        {
            Snackbar.Add("Selecione as duas datas para consultar.", Severity.Warning);
            return;
        }

        if (_rangeEndDate < _rangeStartDate)
        {
            Snackbar.Add("A data final deve ser maior ou igual à data inicial.", Severity.Warning);
            return;
        }

        _isBusy = true;
        StateHasChanged();

        var result = await Handler.GetDashboardMetricsByRangeAsync(_rangeStartDate.Value, _rangeEndDate.Value);

        if (result.IsSuccess && result.Data != null)
            _metrics = result.Data;
        else
            Snackbar.Add(result.Message ?? "Erro ao carregar métricas.", Severity.Error);

        _isBusy = false;
        StateHasChanged();
    }

    private async Task SavePartPurchaseCostAsync(DeliveredProductDto product, decimal newValue)
    {
        if (newValue < 0)
        {
            Snackbar.Add("O valor da peça não pode ser negativo.", Severity.Warning);
            return;
        }

        if (newValue == product.PartPurchaseCost)
            return;

        var result = await Handler.UpdatePartPurchaseCostAsync(new UpdatePartPurchaseCostCommand(product.Id, newValue));

        if (!result.IsSuccess)
        {
            Snackbar.Add(result.Message ?? "Erro ao atualizar o valor da peça.", Severity.Error);
            return;
        }

        Snackbar.Add("Valor da peça atualizado com sucesso!", Severity.Success);

        if (_isRangeMode)
            await LoadByRangeAsync();
        else
            await LoadAsync();
    }

    private static string FormatCurrency(decimal value) =>
        value.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
}
