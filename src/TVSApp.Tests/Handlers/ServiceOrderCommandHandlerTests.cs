using TVS_App.Application.Handlers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Application.Requests.WhatsAppQueueRequests;
using TVS_App.Domain.Enums;
using TVSApp.Tests.Handlers.Fakes;

namespace TVSApp.Tests.Handlers;

[TestClass]
public class ServiceOrderCommandHandlerTests
{
    private FakeServiceOrderQueryRepository _queryRepository = null!;
    private FakeServiceOrderCommandRepository _commandRepository = null!;
    private FakeWhatsAppQueue _whatsAppQueue = null!;
    private ServiceOrderCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _queryRepository = new FakeServiceOrderQueryRepository();
        _commandRepository = new FakeServiceOrderCommandRepository();
        _whatsAppQueue = new FakeWhatsAppQueue();
        _handler = new ServiceOrderCommandHandler(_queryRepository, _commandRepository, _whatsAppQueue);
    }

    private static AddServiceOrderEstimateCommand ValidCommand(
        string warranty = "90 dias",
        decimal partPurchaseCost = 0,
        ERepairResult repairResult = ERepairResult.Repair)
        => new(
            ServiceOrderId: 1,
            Warranty: warranty,
            Solution: "troca da placa",
            TotalCost: 350m,
            RepairResult: repairResult,
            EstimateMessage: "Orçamento da sua TV",
            PartPurchaseCost: partPurchaseCost);

    [TestMethod]
    public async Task AddEstimate_ServiceOrderNotFound_Returns404()
    {
        _queryRepository.ServiceOrderForUpdate = null;

        var result = await _handler.AddEstimate(ValidCommand());

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    [TestMethod]
    public async Task AddEstimate_ValidCommand_UpdatesServiceOrderAndReturns200()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.AddEstimate(ValidCommand());

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Orçamento adicionado com sucesso!", result.Message);
        Assert.IsNotNull(result.Data);
        Assert.AreSame(serviceOrder, _commandRepository.LastUpdated);
        Assert.AreEqual("TROCA DA PLACA", serviceOrder.Solution!.ServiceOrderSolution);
        Assert.AreEqual(EServiceOrderStatus.Evaluated, serviceOrder.ServiceOrderStatus);
        Assert.AreEqual(ERepairStatus.Waiting, serviceOrder.RepairStatus);
    }

    [TestMethod]
    public async Task AddEstimate_Warranty_IsNormalizedAndStoredAsGuarantee()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        await _handler.AddEstimate(ValidCommand(warranty: " 90 dias "));

        Assert.AreEqual("90 DIAS", serviceOrder.Guarantee!.ServiceOrderGuarantee);
    }

    [TestMethod]
    public async Task AddEstimate_PartPurchaseCost_IsApplied()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        await _handler.AddEstimate(ValidCommand(partPurchaseCost: 120m));

        Assert.AreEqual(120m, serviceOrder.PartPurchaseCost.ServiceOrderPartPurchaseCost);
    }

    [TestMethod]
    public async Task AddEstimate_NonPhilcoEnterprise_EnqueuesWhatsAppMessage()
    {
        var serviceOrder = ServiceOrderFactory.Create(EEnterprise.Particular);
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        await _handler.AddEstimate(ValidCommand());

        Assert.AreEqual(1, _whatsAppQueue.Enqueued.Count);
        var message = _whatsAppQueue.Enqueued[0] as SendEstimateMessageRequest;
        Assert.IsNotNull(message);
        Assert.AreEqual(serviceOrder.Id.ToString(), message.ServiceOrder);
        Assert.AreEqual(serviceOrder.SecurityCode, message.SecurityCode);
        Assert.AreEqual(ERepairResult.Repair, message.Result);
        Assert.AreEqual("JOÃO DA SILVA", message.CustomerName);
        Assert.AreEqual("ORÇAMENTO DA SUA TV", message.Solution);
        Assert.AreEqual("41999999999", message.PhoneNumber);
    }

    [TestMethod]
    public async Task AddEstimate_PhilcoBritania_DoesNotEnqueueWhatsAppMessage()
    {
        _queryRepository.ServiceOrderForUpdate = ServiceOrderFactory.Create(EEnterprise.PhilcoBritania);

        var result = await _handler.AddEstimate(ValidCommand());

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }
}
