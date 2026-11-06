using TVS_App.Application.Handlers;
using TVSApp.Tests.Handlers.Fakes;

namespace TVSApp.Tests.Handlers;

[TestClass]
public class ServiceOrderQueryHandlerTests
{
    private FakeServiceOrderQueryRepository _repository = null!;
    private ServiceOrderQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeServiceOrderQueryRepository();
        _handler = new ServiceOrderQueryHandler(_repository);
    }

    [TestMethod]
    public async Task GetPendingEstimateServiceOrders_CallsMatchingRepositoryMethod()
    {
        await _handler.GetPendingEstimateServiceOrders(2, 10);
        AssertCalled(nameof(FakeServiceOrderQueryRepository.GetPendingEstimateAsync), 2, 10);
    }

    [TestMethod]
    public async Task GetAwaitingResponseServiceOrders_CallsMatchingRepositoryMethod()
    {
        await _handler.GetAwaitingResponseServiceOrders(2, 10);
        AssertCalled(nameof(FakeServiceOrderQueryRepository.GetAwaitingResponseAsync), 2, 10);
    }

    [TestMethod]
    public async Task GetApprovedServiceOrders_CallsMatchingRepositoryMethod()
    {
        await _handler.GetApprovedServiceOrders(2, 10);
        AssertCalled(nameof(FakeServiceOrderQueryRepository.GetApprovedAsync), 2, 10);
    }

    [TestMethod]
    public async Task GetRejectedServiceOrders_CallsMatchingRepositoryMethod()
    {
        await _handler.GetRejectedServiceOrders(2, 10);
        AssertCalled(nameof(FakeServiceOrderQueryRepository.GetRejectedAsync), 2, 10);
    }

    [TestMethod]
    public async Task GetAwaitingPartServiceOrders_CallsMatchingRepositoryMethod()
    {
        await _handler.GetAwaitingPartServiceOrders(2, 10);
        AssertCalled(nameof(FakeServiceOrderQueryRepository.GetAwaitingPartAsync), 2, 10);
    }

    [TestMethod]
    public async Task GetReadyForPickupServiceOrders_CallsMatchingRepositoryMethod()
    {
        await _handler.GetReadyForPickupServiceOrders(2, 10);
        AssertCalled(nameof(FakeServiceOrderQueryRepository.GetReadyForPickupAsync), 2, 10);
    }

    private void AssertCalled(string expectedMethod, int pageNumber, int pageSize)
    {
        Assert.AreEqual(expectedMethod, _repository.LastQueueMethod);
        Assert.AreEqual((pageNumber, pageSize), _repository.LastPagination);
    }
}

[TestClass]
public class ServiceOrderSearchTests
{
    private FakeServiceOrderQueryRepository _repository = null!;
    private ServiceOrderQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeServiceOrderQueryRepository();
        _handler = new ServiceOrderQueryHandler(_repository);
    }

    private static TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries.ServiceOrderSearchQuery Query(
        long? number = null, string? customer = null, string? model = null, string? serial = null,
        DateTime? from = null, DateTime? to = null, int pageNumber = 1, int pageSize = 20)
        => new(number, customer, model, serial, from, to, pageNumber, pageSize);

    private async Task AssertBadRequest(
        TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries.ServiceOrderSearchQuery query, string message)
    {
        var result = await _handler.Search(query);

        Assert.AreEqual(400, result.Code);
        Assert.AreEqual(message, result.Message);
        Assert.AreEqual(0, _repository.SearchCalls);
    }

    [TestMethod]
    public async Task NoFilter_ListsLatestOrders()
    {
        var result = await _handler.Search(Query(customer: "   ", pageNumber: 3));

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(1, _repository.SearchCalls);
        Assert.AreEqual(new TVS_App.Domain.Repositories.ServiceOrders.ServiceOrderSearchFilter(null, null, null, null, null, null),
            _repository.LastSearchFilter);
        Assert.AreEqual((3, 20), _repository.LastPagination);
    }

    [TestMethod]
    public Task CustomerAndModel_Returns400() => AssertBadRequest(Query(customer: "maria", model: "lg"), "Preencha um filtro por vez.");

    [TestMethod]
    public Task NumberAndPeriod_Returns400()
        => AssertBadRequest(Query(number: 10, from: new DateTime(2026, 10, 1)), "Preencha um filtro por vez.");

    [TestMethod]
    public Task NumberZero_Returns400() => AssertBadRequest(Query(number: 0), "Informe um número de OS válido.");

    [TestMethod]
    [DataRow("ma")]
    [DataRow("  ma  ")]
    public Task CustomerShorterThan3_Returns400(string customer)
        => AssertBadRequest(Query(customer: customer), "Digite pelo menos 3 letras do cliente.");

    [TestMethod]
    public Task EndBeforeStart_Returns400()
        => AssertBadRequest(Query(from: new DateTime(2026, 10, 10), to: new DateTime(2026, 10, 1)), "A data final não pode ser antes da inicial.");

    [TestMethod]
    [DataRow(0, 20)]
    [DataRow(1, 0)]
    [DataRow(1, 101)]
    public Task InvalidPagination_Returns400(int pageNumber, int pageSize)
        => AssertBadRequest(Query(model: "lg", pageNumber: pageNumber, pageSize: pageSize), "Paginação inválida.");

    [TestMethod]
    public async Task Texts_AreTrimmedBeforeSearching()
    {
        await _handler.Search(Query(model: " lg "));

        Assert.AreEqual("lg", _repository.LastSearchFilter!.Model);
        Assert.IsNull(_repository.LastSearchFilter.Customer);
        Assert.IsNull(_repository.LastSearchFilter.SerialNumber);
    }

    [TestMethod]
    public async Task OnlyFrom_SearchesUntilToday()
    {
        await _handler.Search(Query(from: new DateTime(2026, 10, 1, 15, 30, 0)));

        Assert.AreEqual(new DateTime(2026, 10, 1), _repository.LastSearchFilter!.From);
        Assert.AreEqual(DateTime.Today, _repository.LastSearchFilter.To);
    }

    [TestMethod]
    public async Task OnlyTo_SearchesFromTheBeginning()
    {
        await _handler.Search(Query(to: new DateTime(2026, 10, 31, 23, 59, 0)));

        Assert.IsNull(_repository.LastSearchFilter!.From);
        Assert.AreEqual(new DateTime(2026, 10, 31), _repository.LastSearchFilter.To);
    }

    [TestMethod]
    public async Task Valid_PassesPaginationAndReturnsRepositoryPage()
    {
        var items = new List<TVS_App.Domain.Repositories.ServiceOrderSearchItem>
        {
            new(4312, "MARIA SOUZA", TVS_App.Domain.Enums.EProduct.Tv, "LG", "42LB5600", "SN123",
                TVS_App.Domain.Enums.EEnterprise.Particular, TVS_App.Domain.Enums.EServiceOrderStatus.Delivered, false, DateTime.UtcNow)
        };
        _repository.SearchResponse = new(new(items, 1, 2, 20, 1));

        var result = await _handler.Search(Query(customer: "mar", pageNumber: 2));

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("1 ordens encontradas.", result.Message);
        Assert.AreEqual(4312, result.Data!.Items.Single().Number);
        Assert.AreEqual((2, 20), _repository.LastPagination);
        Assert.AreEqual("mar", _repository.LastSearchFilter!.Customer);
    }

    [TestMethod]
    public async Task NoResults_ReturnsFriendlyMessage()
    {
        var result = await _handler.Search(Query(number: 99999));

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Nenhuma ordem de serviço encontrada.", result.Message);
        Assert.AreEqual(99999, _repository.LastSearchFilter!.Number);
    }
}

[TestClass]
public class ServiceOrderPublicAndPaymentQueryTests
{
    private FakeServiceOrderQueryRepository _repository = null!;
    private ServiceOrderQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeServiceOrderQueryRepository();
        _handler = new ServiceOrderQueryHandler(_repository);
    }

    [TestMethod]
    public async Task GetForCustomer_RightCode_ReturnsEstimate()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        _repository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.GetForCustomer(1, " " + serviceOrder.SecurityCode.ToLower() + " ");

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(1, result.Data!.Id);
        Assert.AreEqual("JOÃO DA SILVA", result.Data.CustomerName);
        Assert.AreEqual("42LB5600", result.Data.ProductModel);
        Assert.AreEqual("NÃO LIGA", result.Data.ProductDefect);
    }

    [TestMethod]
    [DataRow("ZZZZ")]
    [DataRow("")]
    public async Task GetForCustomer_WrongCode_Returns404(string code)
    {
        _repository.ServiceOrderForUpdate = ServiceOrderFactory.Create();

        var result = await _handler.GetForCustomer(1, code);

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
        Assert.IsNull(result.Data);
    }

    [TestMethod]
    public async Task GetForCustomer_UnknownOrder_Returns404()
    {
        _repository.ServiceOrderForUpdate = null;

        var result = await _handler.GetForCustomer(99, "A1B2");

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
    }

    [TestMethod]
    public async Task GetPayments_ReturnsSimpleList()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        serviceOrder.AddPayment(new TVS_App.Domain.Entities.Payment(120m, new DateTime(2026, 11, 2), 1) { Id = 3 });
        _repository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.GetPayments(1);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(1, result.Data!.Count);
        Assert.AreEqual(new TVS_App.Application.DTOs.ServiceOrder.PaymentDto(3, 120m, new DateTime(2026, 11, 2)), result.Data[0]);
    }

    [TestMethod]
    public async Task GetPayments_UnknownOrder_Returns404()
    {
        _repository.ServiceOrderForUpdate = null;

        var result = await _handler.GetPayments(99);

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
    }
}
