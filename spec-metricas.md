# Spec — Dashboard de Métricas

## Contexto

Ordens de venda são identificadas pelo campo `Product.Defect == "VENDA"` (hardcoded em `CreateSalesServiceOrderHandler.cs:37`).
Ordens de reparo são todas as demais (`Product.Defect != "VENDA"`).

---

## Métricas a implementar

### KPI Cards

| Métrica | Filtro |
|---|---|
| Total de ordens (todas + este mês) | todas |
| Ordens ativas | `ServiceOrderStatus != Delivered` |
| Receita total (todas + este mês) | soma de `TotalAmount` |
| Taxa de aprovação de orçamentos | `Approved / (Approved + Disapproved)` em `RepairStatus` |
| Tempo médio de ciclo | média de dias entre `EntryDate` e `DeliveryDate` |
| Total de vendas (todas + este mês) | `Defect == "VENDA"` |
| Receita de vendas (total + este mês) | `Defect == "VENDA"`, soma de `TotalAmount` |

### Gráficos

| Gráfico | Tipo | Filtro |
|---|---|---|
| Ordens por status | Donut | todas |
| Receita por mês (últimos 12m) | Linha | `Defect != "VENDA"` |
| Ordens criadas por mês (últimos 12m) | Linha | `Defect != "VENDA"` |
| Ordens por tipo de produto — Reparos | Barra | `Defect != "VENDA"` |
| Ordens por tipo de produto — Vendas | Barra | `Defect == "VENDA"` |
| Ordens por empresa — Reparos | Barra | `Defect != "VENDA"` |
| Ordens por empresa — Vendas | Barra | `Defect == "VENDA"` |
| Resultado dos reparos | Donut | `RepairResult != null` |
| Receita de vendas por mês (últimos 12m) | Linha | `Defect == "VENDA"` |

### Tabela

| Tabela | Colunas |
|---|---|
| Top 10 clientes | Nome, Qtd de ordens, Total gasto |

---

## Arquitetura — arquivos a criar/editar

### Ordem de implementação (por dependência)

| # | Ação | Arquivo |
|---|---|---|
| 1 | criar | `Src/TVSApp.Application/DTOs/Dashboard/DashboardMetricsDto.cs` |
| 2 | criar | `Src/TVSApp.Application/Requests/MetricsRequests/GetDashboardMetricsQuery.cs` |
| 3 | criar | `Src/TVSApp.Domain/Repositories/Metrics/IMetricsQueryRepository.cs` |
| 4 | criar | `Src/TVSApp.Infrastructure/Repositories/Metrics/MetricsQueryRepository.cs` |
| 5 | criar | `Src/TVSApp.Application/Handlers/Metrics/GetDashboardMetricsHandler.cs` |
| 6 | criar | `Src/TVSApp.Api/Endpoints/DashboardEndpoints.cs` |
| 7a | editar | `Src/TVSApp.Api/Common/BuilderExtensions.cs` |
| 7b | editar | `Src/TVSApp.Api/Common/AppExtensions.cs` |
| 8 | criar | `Src/TVSApp.Web/Handlers/DashboardHandler.cs` |
| 9 | editar | `Src/TVSApp.Web/Common/BuilderExtension.cs` |
| 10 | criar | `Src/TVSApp.Web/Pages/DashboardPage.razor.cs` |
| 11 | criar | `Src/TVSApp.Web/Pages/DashboardPage.razor` |
| 12 | editar | `Src/TVSApp.Web/Layout/MainLayout.razor` (nav link) |

---

## Detalhes por arquivo

### 1. `DashboardMetricsDto.cs`

```csharp
public class DashboardMetricsDto
{
    // KPIs gerais
    public int TotalOrders { get; set; }
    public int TotalOrdersThisMonth { get; set; }
    public int ActiveOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public double ApprovalRate { get; set; }
    public double AverageCycleDays { get; set; }

    // KPIs de vendas
    public int TotalSales { get; set; }
    public int TotalSalesThisMonth { get; set; }
    public decimal TotalSalesRevenue { get; set; }
    public decimal SalesRevenueThisMonth { get; set; }

    // Gráficos — cada gráfico tem Labels + Values
    public List<string> OrdersByStatusLabels { get; set; } = [];
    public List<double> OrdersByStatusValues { get; set; } = [];

    public List<string> RevenueByMonthLabels { get; set; } = [];
    public List<double> RevenueByMonthValues { get; set; } = [];

    public List<string> OrdersCreatedPerMonthLabels { get; set; } = [];
    public List<double> OrdersCreatedPerMonthValues { get; set; } = [];

    public List<string> RepairsByProductTypeLabels { get; set; } = [];
    public List<double> RepairsByProductTypeValues { get; set; } = [];

    public List<string> SalesByProductTypeLabels { get; set; } = [];
    public List<double> SalesByProductTypeValues { get; set; } = [];

    public List<string> RepairsByEnterpriseLabels { get; set; } = [];
    public List<double> RepairsByEnterpriseValues { get; set; } = [];

    public List<string> SalesByEnterpriseLabels { get; set; } = [];
    public List<double> SalesByEnterpriseValues { get; set; } = [];

    public List<string> RepairResultLabels { get; set; } = [];
    public List<double> RepairResultValues { get; set; } = [];

    public List<string> SalesRevenueByMonthLabels { get; set; } = [];
    public List<double> SalesRevenueByMonthValues { get; set; } = [];

    // Tabela
    public List<TopCustomerDto> TopCustomers { get; set; } = [];
}

public class TopCustomerDto
{
    public string CustomerName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}
```

---

### 2. `GetDashboardMetricsQuery.cs`

```csharp
public class GetDashboardMetricsQuery : IRequest<BaseResponse<DashboardMetricsDto>> { }
```

---

### 3. `IMetricsQueryRepository.cs`

```csharp
public interface IMetricsQueryRepository
{
    Task<DashboardMetricsDto> GetDashboardMetricsAsync();
}
```

---

### 4. `MetricsQueryRepository.cs`

Injeta `ApplicationDataContext`. Carrega todas as ordens com includes necessários em memória, depois agrega com LINQ.

```csharp
var orders = await _context.ServiceOrders
    .Include(x => x.Customer)
    .Include(x => x.Payments)
    .AsNoTracking()
    .ToListAsync();

var repairs = orders.Where(x => x.Product.Defect != "VENDA").ToList();
var sales   = orders.Where(x => x.Product.Defect == "VENDA").ToList();
var now     = DateTime.UtcNow;
var last12  = Enumerable.Range(0, 12)
                .Select(i => now.AddMonths(-11 + i))
                .ToList();
```

**Agregações:**
- KPIs: contagens/somas/médias diretas sobre as listas
- `ApprovalRate`: `repairs.Count(x => x.RepairStatus == ERepairStatus.Approved) * 100.0 / (aprovados + reprovados)`
- `AverageCycleDays`: `repairs.Where(x => x.DeliveryDate.HasValue).Average(x => (x.DeliveryDate!.Value - x.EntryDate).TotalDays)`
- Séries mensais: iterar `last12`, para cada mês filtrar por `EntryDate.Year == m.Year && EntryDate.Month == m.Month`
- `RepairResults`: `Where(x => x.RepairResult.HasValue).GroupBy(x => x.RepairResult!.Value)`
- `TopCustomers`: `GroupBy(x => x.Customer.Name.CustomerName).OrderByDescending(g => g.Count()).Take(10)`

**Labels em PT-BR para enums:**

```
EServiceOrderStatus: Entrada, Avaliado, Consertado, Entregue, Aguardando Peça, Pronto p/ Retirada
ERepairResult:       Tem conserto, Não tem conserto, Não apresentou defeito
EProduct:            TV, TV Box, Controle Remoto, Som, Caixa Acústica, Microondas, Fonte, Bases, Placa Fonte, Placa Principal
EEnterprise:         Particular, Cocel, Seguradora, Philco/Britania, Copel
```

> ⚠️ `TotalAmount` é propriedade calculada — não pode ser traduzida para SQL. Sempre chamar `ToListAsync()` antes de agregar.

---

### 5. `GetDashboardMetricsHandler.cs`

```csharp
public class GetDashboardMetricsHandler(IMetricsQueryRepository repository)
    : IRequestHandler<GetDashboardMetricsQuery, BaseResponse<DashboardMetricsDto>>
{
    public async Task<BaseResponse<DashboardMetricsDto>> Handle(
        GetDashboardMetricsQuery request, CancellationToken cancellationToken)
    {
        var data = await repository.GetDashboardMetricsAsync();
        return new BaseResponse<DashboardMetricsDto>(data, 200, "Métricas carregadas com sucesso.");
    }
}
```

---

### 6. `DashboardEndpoints.cs`

```csharp
public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("get-dashboard-metrics", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetDashboardMetricsQuery());
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
        .RequireAuthorization()
        .WithTags("Dashboard")
        .WithSummary("Retorna todas as métricas do dashboard.");
    }
}
```

---

### 7a. Editar `BuilderExtensions.cs` (API)

Adicionar junto aos outros repositórios:
```csharp
builder.Services.AddScoped<IMetricsQueryRepository, MetricsQueryRepository>();
```

### 7b. Editar `AppExtensions.cs` (API)

Adicionar junto aos outros endpoints:
```csharp
app.MapDashboardEndpoints();
```

---

### 8. `DashboardHandler.cs` (Web)

Segue padrão de `ServiceOrderHandler.cs`:

```csharp
public class DashboardHandler(IHttpClientFactory clientFactory)
{
    private readonly HttpClient _httpClient = clientFactory.CreateClient("api");

    public async Task<BaseResponse<DashboardMetricsDto>> GetDashboardMetricsAsync()
    {
        var response = await _httpClient
            .GetFromJsonAsync<BaseResponse<DashboardMetricsDto>>("get-dashboard-metrics");

        return response ?? new BaseResponse<DashboardMetricsDto>(null, 500, "Erro ao obter métricas.");
    }
}
```

---

### 9. Editar `BuilderExtension.cs` (Web)

```csharp
builder.Services.AddTransient<DashboardHandler>();
```

---

### 10. `DashboardPage.razor.cs`

```csharp
public partial class DashboardPage : ComponentBase
{
    [Inject] public DashboardHandler Handler { get; set; } = null!;
    [Inject] public ISnackbar Snackbar { get; set; } = null!;

    private DashboardMetricsDto? _metrics;
    private bool _isBusy;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _isBusy = true;
        var result = await Handler.GetDashboardMetricsAsync();
        if (result.IsSuccess && result.Data != null)
            _metrics = result.Data;
        else
            Snackbar.Add(result.Message ?? "Erro ao carregar métricas.", Severity.Error);
        _isBusy = false;
    }
}
```

---

### 11. `DashboardPage.razor` (estrutura)

```
@page "/dashboard"
@attribute [Authorize]
@layout MainLayout

- MudProgressCircular enquanto _isBusy

KPI Cards (MudGrid com MudPaper):
  - Total de Ordens / este mês
  - Ordens Ativas
  - Receita Total / este mês
  - Taxa de Aprovação
  - Tempo Médio de Ciclo
  - Total de Vendas / este mês
  - Receita de Vendas / este mês

Linha 1 (MudGrid md=6 cada):
  - MudChart Donut  → Ordens por Status
  - MudChart Line   → Receita de Reparos por Mês

Linha 2:
  - MudChart Bar    → Reparos por Tipo de Produto
  - MudChart Bar    → Vendas por Tipo de Produto

Linha 3:
  - MudChart Bar    → Reparos por Empresa
  - MudChart Bar    → Vendas por Empresa

Linha 4:
  - MudChart Donut  → Resultado dos Reparos
  - MudChart Line   → Receita de Vendas por Mês

Linha 5:
  - MudTable        → Top 10 Clientes (Nome, Ordens, Total Gasto)
```

> **MudChart API:** Donut/Pie usam `InputData` + `InputLabels`. Bar/Line usam `ChartSeries` + `XAxisLabels`.

---

### 12. `MainLayout.razor`

Adicionar nav link:
```razor
<MudNavLink Href="/dashboard" Icon="@Icons.Material.Filled.Dashboard">Dashboard</MudNavLink>
```

---

## Pontos de atenção

- `TotalAmount` é propriedade calculada (`PartCost + LaborCost`) — nunca agregá-la dentro de LINQ-to-SQL
- `RepairResult` é `nullable` — sempre filtrar `Where(x => x.RepairResult.HasValue)` antes do GroupBy
- Incluir `Include(x => x.Customer)` e `Include(x => x.Payments)` no repositório
- `Customer.Name` é um value object — acessar via `Customer.Name.CustomerName`
