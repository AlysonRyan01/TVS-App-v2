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
    private FakeNotificationCommandRepository _notificationRepository = null!;
    private FakeServiceOrderPdfGenerator _pdfGenerator = null!;
    private ServiceOrderCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _queryRepository = new FakeServiceOrderQueryRepository();
        _commandRepository = new FakeServiceOrderCommandRepository();
        _whatsAppQueue = new FakeWhatsAppQueue();
        _notificationRepository = new FakeNotificationCommandRepository();
        _pdfGenerator = new FakeServiceOrderPdfGenerator();
        _handler = new ServiceOrderCommandHandler(
            _queryRepository, _commandRepository, _whatsAppQueue, _notificationRepository, _pdfGenerator);
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

    // ---------- Resposta do orçamento (feature 002) ----------

    private static TVS_App.Domain.Entities.ServiceOrder AwaitingResponse(EEnterprise enterprise = EEnterprise.Particular)
    {
        var serviceOrder = ServiceOrderFactory.Create(enterprise);
        serviceOrder.AddEstimate("TROCA DA PLACA", "90 DIAS", 350m, ERepairResult.Repair, "msg");

        // Na PhilcoBritania o AddEstimate já aprova; força o estado "aguardando resposta" para testar a regra do WhatsApp.
        serviceOrder.EditServiceOrderStatus(EServiceOrderStatus.Evaluated);
        serviceOrder.EditRepairStatus(ERepairStatus.Waiting);
        return serviceOrder;
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Respond_ServiceOrderNotFound_Returns404(bool approve)
    {
        _queryRepository.ServiceOrderForUpdate = null;

        var result = approve ? await _handler.ApproveEstimate(1) : await _handler.RejectEstimate(1);

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
        Assert.AreEqual(0, _notificationRepository.Created.Count);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Respond_AlreadyAnswered_Returns409WithoutSideEffects(bool approve)
    {
        var serviceOrder = AwaitingResponse();
        serviceOrder.ApproveEstimate();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = approve ? await _handler.ApproveEstimate(1) : await _handler.RejectEstimate(1);

        Assert.AreEqual(409, result.Code);
        Assert.AreEqual("Esse orçamento já foi respondido.", result.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
        Assert.AreEqual(0, _notificationRepository.Created.Count);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    [TestMethod]
    public async Task ApproveEstimate_AwaitingResponse_ApprovesNotifiesAndSendsWhatsApp()
    {
        var serviceOrder = AwaitingResponse();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.ApproveEstimate(1);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Aprovação adicionada com sucesso!", result.Message);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(ERepairStatus.Approved, serviceOrder.RepairStatus);
        Assert.AreSame(serviceOrder, _commandRepository.LastUpdated);

        Assert.AreEqual(1, _notificationRepository.Created.Count);
        Assert.AreEqual(("Orçamento Aprovado, OS: 1", "Favor comprar peça para OS: 1"), _notificationRepository.Created[0]);

        Assert.AreEqual(2, _whatsAppQueue.Enqueued.Count);
        AssertOwnerNotified(_whatsAppQueue.Enqueued[1], ERepairStatus.Approved);
        var message = _whatsAppQueue.Enqueued[0] as SendThankYouForEstimateApprovalRequest;
        Assert.IsNotNull(message);
        Assert.AreEqual("1", message.ServiceOrder);
        Assert.AreEqual("JOÃO DA SILVA", message.CustomerName);
        Assert.AreEqual("41999999999", message.PhoneNumber);
        Assert.AreEqual(serviceOrder.TotalAmount, message.Amount);
    }

    [TestMethod]
    public async Task RejectEstimate_AwaitingResponse_RejectsNotifiesAndSendsWhatsApp()
    {
        var serviceOrder = AwaitingResponse();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.RejectEstimate(1);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Orçamento reprovado com sucesso!", result.Message);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(ERepairStatus.Disapproved, serviceOrder.RepairStatus);
        Assert.AreSame(serviceOrder, _commandRepository.LastUpdated);

        Assert.AreEqual(1, _notificationRepository.Created.Count);
        Assert.AreEqual(("Orçamento Reprovado, OS: 1", "Favor montar a OS 1 para entrega"), _notificationRepository.Created[0]);

        Assert.AreEqual(2, _whatsAppQueue.Enqueued.Count);
        AssertOwnerNotified(_whatsAppQueue.Enqueued[1], ERepairStatus.Disapproved);
        var message = _whatsAppQueue.Enqueued[0] as SendEstimateDisapprovalAcknowledgmentRequest;
        Assert.IsNotNull(message);
        Assert.AreEqual("1", message.ServiceOrder);
        Assert.AreEqual("JOÃO DA SILVA", message.CustomerName);
        Assert.AreEqual("41999999999", message.PhoneNumber);
    }

    [TestMethod]
    public async Task ApproveEstimate_PhilcoBritania_NotifiesWithoutWhatsApp()
    {
        _queryRepository.ServiceOrderForUpdate = AwaitingResponse(EEnterprise.PhilcoBritania);

        var result = await _handler.ApproveEstimate(1);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(1, _notificationRepository.Created.Count);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    [TestMethod]
    public async Task RejectEstimate_PhilcoBritania_StillSendsWhatsApp()
    {
        _queryRepository.ServiceOrderForUpdate = AwaitingResponse(EEnterprise.PhilcoBritania);

        var result = await _handler.RejectEstimate(1);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(1, _notificationRepository.Created.Count);
        Assert.AreEqual(2, _whatsAppQueue.Enqueued.Count);
        Assert.IsInstanceOfType(_whatsAppQueue.Enqueued[0], typeof(SendEstimateDisapprovalAcknowledgmentRequest));
        AssertOwnerNotified(_whatsAppQueue.Enqueued[1], ERepairStatus.Disapproved);
    }

    // ---------- Compra da peça (feature 003) ----------

    [TestMethod]
    public async Task RegisterPartPurchase_ServiceOrderNotFound_Returns404()
    {
        _queryRepository.ServiceOrderForUpdate = null;

        var result = await _handler.RegisterPartPurchase(1);

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
    }

    [TestMethod]
    public async Task RegisterPartPurchase_ApprovedServiceOrder_RegistersPurchase()
    {
        var serviceOrder = AwaitingResponse();
        serviceOrder.ApproveEstimate();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.RegisterPartPurchase(1);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Compra de peça adicionada com sucesso!", result.Message);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(EServiceOrderStatus.OrderPart, serviceOrder.ServiceOrderStatus);
        Assert.IsNotNull(serviceOrder.PurchasePartDate);
        Assert.AreSame(serviceOrder, _commandRepository.LastUpdated);
        Assert.AreEqual(0, _notificationRepository.Created.Count);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    // ---------- Liberação de OS reprovada (feature 004) ----------

    private static TVS_App.Domain.Entities.ServiceOrder Rejected(EEnterprise enterprise = EEnterprise.Particular)
    {
        var serviceOrder = AwaitingResponse(enterprise);
        serviceOrder.RejectEstimate();
        return serviceOrder;
    }

    [TestMethod]
    public async Task MarkRejected_IdZero_Returns400WithoutQuerying()
    {
        var result = await _handler.MarkRejectedEstimateAsReadyForPickup(0);

        Assert.AreEqual(400, result.Code);
        Assert.AreEqual("O número da ordem de serviço não pode ser 0", result.Message);
        Assert.IsFalse(_queryRepository.GetByIdForUpdateCalled);
    }

    [TestMethod]
    public async Task MarkRejected_NotFound_Returns404WithRepositoryMessage()
    {
        _queryRepository.ServiceOrderForUpdate = null;

        var result = await _handler.MarkRejectedEstimateAsReadyForPickup(5);

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("A ordem de serviço com ID:5 não existe", result.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
    }

    [TestMethod]
    public async Task MarkRejected_NotRejected_Throws()
    {
        _queryRepository.ServiceOrderForUpdate = AwaitingResponse();

        await Assert.ThrowsExceptionAsync<TVS_App.Domain.Exceptions.EntityException<TVS_App.Domain.Entities.ServiceOrder>>(
            () => _handler.MarkRejectedEstimateAsReadyForPickup(1));

        Assert.IsNull(_commandRepository.LastUpdated);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    [TestMethod]
    public async Task MarkRejected_UpdateFails_ReturnsUpdateErrorWithoutWhatsApp()
    {
        _queryRepository.ServiceOrderForUpdate = Rejected();
        _commandRepository.UpdateResponse = new TVS_App.Domain.Shared.BaseResponse<TVS_App.Domain.Entities.ServiceOrder?>(null, 500, "Erro ao salvar");

        var result = await _handler.MarkRejectedEstimateAsReadyForPickup(1);

        Assert.AreEqual(500, result.Code);
        Assert.AreEqual("Erro ao salvar", result.Message);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    [TestMethod]
    [DataRow(EEnterprise.Particular)]
    [DataRow(EEnterprise.PhilcoBritania)]
    public async Task MarkRejected_Rejected_MarksReadyAndSendsWhatsApp(EEnterprise enterprise)
    {
        var serviceOrder = Rejected(enterprise);
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.MarkRejectedEstimateAsReadyForPickup(1);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Ordem de serviço marcada como pronta para entrega", result.Message);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(EServiceOrderStatus.ReadyToPickUp, serviceOrder.ServiceOrderStatus);
        Assert.AreEqual(ERepairStatus.Disapproved, serviceOrder.RepairStatus);
        Assert.AreSame(serviceOrder, _commandRepository.LastUpdated);
        Assert.AreEqual(0, _notificationRepository.Created.Count);

        Assert.AreEqual(1, _whatsAppQueue.Enqueued.Count);
        var message = _whatsAppQueue.Enqueued[0] as SendRejectionReadyForPickupMessageRequest;
        Assert.IsNotNull(message);
        Assert.AreEqual("JOÃO DA SILVA", message.CustomerName);
        Assert.AreEqual("41999999999", message.PhoneNumber);
    }

    // ---------- Conserto (feature 005) ----------

    private static TVS_App.Domain.Entities.ServiceOrder AwaitingPart(EEnterprise enterprise = EEnterprise.Particular)
    {
        var serviceOrder = AwaitingResponse(enterprise);
        serviceOrder.ApproveEstimate();
        serviceOrder.AddPurchasedPart();
        return serviceOrder;
    }

    [TestMethod]
    public async Task RegisterRepair_NotFound_Returns404()
    {
        _queryRepository.ServiceOrderForUpdate = null;

        var result = await _handler.RegisterRepair(1);

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
    }

    [TestMethod]
    public async Task RegisterRepair_EstimateNotApproved_Throws()
    {
        _queryRepository.ServiceOrderForUpdate = AwaitingResponse();

        await Assert.ThrowsExceptionAsync<TVS_App.Domain.Exceptions.EntityException<TVS_App.Domain.Entities.ServiceOrder>>(
            () => _handler.RegisterRepair(1));

        Assert.IsNull(_commandRepository.LastUpdated);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    [TestMethod]
    [DataRow(EEnterprise.Particular)]
    [DataRow(EEnterprise.PhilcoBritania)]
    public async Task RegisterRepair_AwaitingPart_RepairsAndSendsWhatsApp(EEnterprise enterprise)
    {
        var serviceOrder = AwaitingPart(enterprise);
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.RegisterRepair(1);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Conserto adicionado com sucesso", result.Message);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(EServiceOrderStatus.Repaired, serviceOrder.ServiceOrderStatus);
        Assert.IsNotNull(serviceOrder.RepairDate);
        Assert.AreSame(serviceOrder, _commandRepository.LastUpdated);
        Assert.AreEqual(0, _notificationRepository.Created.Count);

        Assert.AreEqual(1, _whatsAppQueue.Enqueued.Count);
        var message = _whatsAppQueue.Enqueued[0] as SendDeviceReadyMessageRequest;
        Assert.IsNotNull(message);
        Assert.AreEqual("1", message.ServiceOrder);
        Assert.AreEqual("JOÃO DA SILVA", message.CustomerName);
        Assert.AreEqual("41999999999", message.PhoneNumber);
    }

    // O handler antigo não checa o resultado do salvamento: responde 200 e envia o WhatsApp mesmo assim.
    [TestMethod]
    public async Task RegisterRepair_UpdateFails_StillReturns200AndSendsWhatsApp()
    {
        _queryRepository.ServiceOrderForUpdate = AwaitingPart();
        _commandRepository.UpdateResponse = new TVS_App.Domain.Shared.BaseResponse<TVS_App.Domain.Entities.ServiceOrder?>(null, 500, "Erro ao salvar");

        var result = await _handler.RegisterRepair(1);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(1, _whatsAppQueue.Enqueued.Count);
    }

    // ---------- Entrega (feature 006) ----------

    private static TVS_App.Domain.Entities.ServiceOrder RepairedOs(EEnterprise enterprise = EEnterprise.Particular)
    {
        var serviceOrder = AwaitingPart(enterprise);
        serviceOrder.ExecuteRepair();
        return serviceOrder;
    }

    [TestMethod]
    public async Task RegisterDelivery_NotFound_Returns404()
    {
        _queryRepository.ServiceOrderForUpdate = null;

        var result = await _handler.RegisterDelivery(1);

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
        Assert.AreEqual(0, _pdfGenerator.CheckOutCalls);
    }

    [TestMethod]
    [DataRow(EEnterprise.Particular)]
    [DataRow(EEnterprise.PhilcoBritania)]
    public async Task RegisterDelivery_Repaired_ReturnsPdfAndSendsGuarantee(EEnterprise enterprise)
    {
        var serviceOrder = RepairedOs(enterprise);
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.RegisterDelivery(1);

        Assert.AreEqual(200, result.Code);
        CollectionAssert.AreEqual(_pdfGenerator.CheckOutResponse.Data, result.Data);
        Assert.AreEqual(EServiceOrderStatus.Delivered, serviceOrder.ServiceOrderStatus);
        Assert.IsNotNull(serviceOrder.DeliveryDate);
        Assert.AreSame(serviceOrder, _commandRepository.LastUpdated);
        Assert.AreEqual(1, _pdfGenerator.CheckOutCalls);
        Assert.AreEqual(0, _notificationRepository.Created.Count);

        Assert.AreEqual(1, _whatsAppQueue.Enqueued.Count);
        var message = _whatsAppQueue.Enqueued[0] as SendGuaranteeMessageRequest;
        Assert.IsNotNull(message);
        Assert.AreEqual("1", message.ServiceOrder);
        Assert.AreEqual("JOÃO DA SILVA", message.CustomerName);
        Assert.AreEqual("90 DIAS", message.Guarantee);
        Assert.AreEqual("41999999999", message.PhoneNumber);
    }

    // O handler antigo salva a entrega antes de gerar o PDF: se o PDF falhar, a entrega continua registrada.
    [TestMethod]
    public async Task RegisterDelivery_Repaired_PdfFails_Returns500AndKeepsDelivery()
    {
        var serviceOrder = RepairedOs();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;
        _pdfGenerator.CheckOutResponse = new TVS_App.Domain.Shared.BaseResponse<byte[]>(null, 500, "Falha no PDF");

        var result = await _handler.RegisterDelivery(1);

        Assert.AreEqual(500, result.Code);
        Assert.AreEqual("Falha no PDF", result.Message);
        Assert.AreEqual(EServiceOrderStatus.Delivered, serviceOrder.ServiceOrderStatus);
        Assert.AreSame(serviceOrder, _commandRepository.LastUpdated);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    [TestMethod]
    public async Task RegisterDelivery_NotRepaired_DeliversWithoutPdf()
    {
        var serviceOrder = Rejected();
        serviceOrder.MarkEstimateRejectedAsReadyToPickUp();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.RegisterDelivery(1);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Ordem de serviço entregue com sucesso!", result.Message);
        Assert.IsNull(result.Data);
        Assert.AreEqual(EServiceOrderStatus.Delivered, serviceOrder.ServiceOrderStatus);
        Assert.AreEqual(0, _pdfGenerator.CheckOutCalls);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    // ---------- Localização do produto ----------

    [TestMethod]
    public async Task AddProductLocation_NotFound_Returns404()
    {
        _queryRepository.ServiceOrderForUpdate = null;

        var result = await _handler.AddProductLocation(new AddProductLocationCommand(1, "A1"));

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
    }

    [TestMethod]
    [DataRow(0L, "A1")]
    [DataRow(1L, "   ")]
    public async Task AddProductLocation_InvalidCommand_Throws(long id, string location)
    {
        _queryRepository.ServiceOrderForUpdate = ServiceOrderFactory.Create();

        await Assert.ThrowsExceptionAsync<TVS_App.Application.Exceptions.CommandException<AddProductLocationCommand>>(
            () => _handler.AddProductLocation(new AddProductLocationCommand(id, location)));

        Assert.IsNull(_commandRepository.LastUpdated);
    }

    [TestMethod]
    public async Task AddProductLocation_Valid_SavesNormalizedLocation()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.AddProductLocation(new AddProductLocationCommand(1, "  prateleira b3 "));

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Localização do produto adicionada com sucesso!", result.Message);
        Assert.AreEqual("PRATELEIRA B3", result.Data);
        Assert.AreEqual("PRATELEIRA B3", serviceOrder.Product.Location);
        Assert.AreSame(serviceOrder, _commandRepository.LastUpdated);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    private static void AssertOwnerNotified(object request, ERepairStatus expected)
    {
        var owner = request as NotifyOwnerEstimateDecisionRequest;
        Assert.IsNotNull(owner);
        Assert.AreEqual("1", owner.ServiceOrder);
        Assert.AreEqual(expected, owner.Result);
        Assert.AreEqual("JOÃO DA SILVA", owner.CustomerName);
    }
}
