using TVS_App.Application.Handlers.Metrics;
using TVSApp.Tests.Handlers.Fakes;

namespace TVSApp.Tests.Handlers;

[TestClass]
public class MetricsQueryHandlerTests
{
    private FakeMetricsService _service = null!;
    private MetricsQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new FakeMetricsService();
        _handler = new MetricsQueryHandler(_service);
    }

    private async Task AssertBadRequest(int? month, int? year, DateTime? from, DateTime? to, string message)
    {
        var result = await _handler.GetDashboard(month, year, from, to);

        Assert.AreEqual(400, result.Code);
        Assert.AreEqual(message, result.Message);
        Assert.IsNull(_service.LastMonth);
        Assert.IsNull(_service.LastRange);
    }

    [TestMethod]
    public async Task NoPeriod_UsesCurrentMonth()
    {
        var result = await _handler.GetDashboard(null, null, null, null);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Métricas carregadas com sucesso.", result.Message);
        Assert.AreEqual((DateTime.Today.Month, DateTime.Today.Year), _service.LastMonth);
    }

    [TestMethod]
    [DataRow(0, 2026)]
    [DataRow(13, 2026)]
    [DataRow(5, 1999)]
    [DataRow(5, 2101)]
    public Task InvalidMonthOrYear_Returns400(int month, int year)
        => AssertBadRequest(month, year, null, null, "Mês ou ano inválido.");

    [TestMethod]
    public Task OnlyFrom_Returns400()
        => AssertBadRequest(null, null, new DateTime(2026, 11, 1), null, "Informe as duas datas do período.");

    [TestMethod]
    public Task OnlyTo_Returns400()
        => AssertBadRequest(null, null, null, new DateTime(2026, 11, 1), "Informe as duas datas do período.");

    [TestMethod]
    public Task EndBeforeStart_Returns400()
        => AssertBadRequest(null, null, new DateTime(2026, 11, 10), new DateTime(2026, 11, 1), "A data final não pode ser antes da inicial.");

    [TestMethod]
    public async Task Range_IgnoresMonthAndDropsTime()
    {
        var result = await _handler.GetDashboard(3, 2025, new DateTime(2026, 11, 1, 14, 0, 0), new DateTime(2026, 11, 15, 9, 0, 0));

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual((new DateTime(2026, 11, 1), new DateTime(2026, 11, 15)), _service.LastRange);
        Assert.IsNull(_service.LastMonth);
    }

    [TestMethod]
    public async Task MonthAndYear_AreUsed()
    {
        await _handler.GetDashboard(2, 2025, null, null);

        Assert.AreEqual((2, 2025), _service.LastMonth);
    }
}
