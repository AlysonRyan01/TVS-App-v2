using Microsoft.AspNetCore.Components;
using MudBlazor;
using TVS_App.Application.DTOs.Dashboard;
using TVSApp.Web.Handlers;

namespace TVSApp.Web.Pages;

public partial class DashboardPage : ComponentBase
{
    [Inject] public DashboardHandler Handler { get; set; } = null!;
    [Inject] public ISnackbar Snackbar { get; set; } = null!;

    private DashboardMetricsDto? _metrics;
    private bool _isBusy;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _isBusy = true;
        StateHasChanged();

        var result = await Handler.GetDashboardMetricsAsync();

        if (result.IsSuccess && result.Data != null)
            _metrics = result.Data;
        else
            Snackbar.Add(result.Message ?? "Erro ao carregar métricas.", Severity.Error);

        _isBusy = false;
        StateHasChanged();
    }

    private List<ChartSeries> SingleSeries(string name, List<double> values) =>
        [new ChartSeries { Name = name, Data = values.ToArray() }];

    private static string FormatCurrency(decimal value) =>
        value.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
}
