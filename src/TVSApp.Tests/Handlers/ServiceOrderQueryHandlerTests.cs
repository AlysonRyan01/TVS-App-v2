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
