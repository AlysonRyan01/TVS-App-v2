using TVS_App.Domain.Entities;
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

    private static TVS_App.Domain.Entities.ServiceOrder AwaitingResponse(EEnterprise enterprise = EEnterprise.Particular)
    {
        var serviceOrder = ServiceOrderFactory.Create(enterprise);
        serviceOrder.AddEstimate("TROCA DA PLACA", "90 DIAS", 350m, ERepairResult.Repair, "msg");

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

    [TestMethod]
    public async Task RegisterRepair_UpdateFails_StillReturns200AndSendsWhatsApp()
    {
        _queryRepository.ServiceOrderForUpdate = AwaitingPart();
        _commandRepository.UpdateResponse = new TVS_App.Domain.Shared.BaseResponse<TVS_App.Domain.Entities.ServiceOrder?>(null, 500, "Erro ao salvar");

        var result = await _handler.RegisterRepair(1);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(1, _whatsAppQueue.Enqueued.Count);
    }

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

    private static CreateServiceOrderCommand NewOrder(long customerId = 7, string model = " 42lb5600 ", string serial = "sn123")
        => new(customerId, " lg ", model, serial, "não liga", "controle", EProduct.Tv, EEnterprise.Particular);

    [TestMethod]
    [DataRow(0L, "42LB", "SN", "O CustomerId da ordem de serviço não pode ser 0")]
    [DataRow(7L, "", "SN", "O modelo da ordem de serviço não pode estar vazio")]
    [DataRow(7L, "42LB", "", "O número de série da ordem de serviço não pode estar vazio")]
    public async Task CreateServiceOrder_Invalid_Throws(long customerId, string model, string serial, string message)
    {
        var ex = await Assert.ThrowsExceptionAsync<TVS_App.Application.Exceptions.CommandException<CreateServiceOrderCommand>>(
            () => _handler.CreateServiceOrder(NewOrder(customerId, model, serial)));

        Assert.AreEqual(message, ex.Message);
        Assert.IsNull(_commandRepository.LastCreated);
    }

    [TestMethod]
    public async Task CreateServiceOrder_Valid_ReturnsIdAndPdfAndSendsWelcome()
    {
        var result = await _handler.CreateServiceOrder(NewOrder());

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(1, result.Data!.Id);
        CollectionAssert.AreEqual(_pdfGenerator.CheckInResponse.Data, result.Data.Pdf);

        var created = _commandRepository.LastCreated!;
        Assert.AreEqual("42LB5600", created.Product.Model);
        Assert.AreEqual("LG", created.Product.Brand);
        Assert.AreEqual("SN123", created.Product.SerialNumber);
        Assert.AreEqual(EServiceOrderStatus.Entered, created.ServiceOrderStatus);
        Assert.AreEqual(1, _pdfGenerator.CheckInCalls);

        Assert.AreEqual(1, _whatsAppQueue.Enqueued.Count);
        var message = _whatsAppQueue.Enqueued[0] as SendWelcomeMessageRequest;
        Assert.IsNotNull(message);
        Assert.AreEqual("1", message.ServiceOrder);
        Assert.AreEqual("JOÃO DA SILVA", message.CustomerName);
        Assert.AreEqual("41999999999", message.PhoneNumber);
    }

    [TestMethod]
    public async Task CreateServiceOrder_CreateFails_Returns500WithoutPdf()
    {
        _commandRepository.CreateResponse = new TVS_App.Domain.Shared.BaseResponse<TVS_App.Domain.Entities.ServiceOrder?>(null, 404, "Cliente não encontrado");

        var result = await _handler.CreateServiceOrder(NewOrder());

        Assert.AreEqual(500, result.Code);
        Assert.AreEqual("Cliente não encontrado", result.Message);
        Assert.AreEqual(0, _pdfGenerator.CheckInCalls);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    [TestMethod]
    public async Task CreateServiceOrder_PdfFails_Returns500AndKeepsOrder()
    {
        _pdfGenerator.CheckInResponse = new TVS_App.Domain.Shared.BaseResponse<byte[]>(null, 500, "Falha no PDF de entrada");

        var result = await _handler.CreateServiceOrder(NewOrder());

        Assert.AreEqual(500, result.Code);
        Assert.AreEqual("Falha no PDF de entrada", result.Message);
        Assert.IsNotNull(_commandRepository.LastCreated);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    private static CreateSalesServiceOrderCommand NewSale(
        long customerId = 7,
        string model = " un50au7700 ",
        string serial = " abc123 ",
        string guarantee = " 90 dias ",
        decimal amount = 1499.90m,
        EProduct type = EProduct.Tv)
        => new(customerId, " samsung ", model, serial, guarantee, amount, type);

    [TestMethod]
    [DataRow(0L, "UN50", "SN", "90 DIAS", 100.0, 1, "O CustomerId da ordem de serviço não pode ser 0")]
    [DataRow(7L, "", "SN", "90 DIAS", 100.0, 1, "O modelo da ordem de serviço não pode estar vazio")]
    [DataRow(7L, "UN50", "", "90 DIAS", 100.0, 1, "O número de série da ordem de serviço não pode estar vazio")]
    [DataRow(7L, "UN50", "SN", "", 100.0, 1, "A venda precisa ter uma garantia")]
    [DataRow(7L, "UN50", "SN", "90 DIAS", 100.0, 999, "O tipo do produto da ordem de serviço não tem um valor válido")]
    [DataRow(7L, "UN50", "SN", "90 DIAS", 0.0, 1, "O valor da venda não pode ser vazio ou 0")]
    public async Task CreateSalesServiceOrder_Invalid_Throws(
        long customerId, string model, string serial, string guarantee, double amount, int type, string message)
    {
        var ex = await Assert.ThrowsExceptionAsync<TVS_App.Application.Exceptions.CommandException<CreateServiceOrderCommand>>(
            () => _handler.CreateSalesServiceOrder(NewSale(customerId, model, serial, guarantee, (decimal)amount, (EProduct)type)));

        Assert.AreEqual(message, ex.Message);
        Assert.IsNull(_commandRepository.LastCreated);
        Assert.AreEqual(0, _pdfGenerator.SaleCalls);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    [TestMethod]
    public async Task CreateSalesServiceOrder_Valid_CreatesDeliveredSaleOrder()
    {
        await _handler.CreateSalesServiceOrder(NewSale());

        var created = _commandRepository.LastCreated!;
        Assert.AreEqual(EEnterprise.Particular, created.Enterprise);
        Assert.AreEqual("VENDA", created.Product.Defect);
        Assert.AreEqual("", created.Product.Accessories);
        Assert.AreEqual("SAMSUNG", created.Product.Brand);
        Assert.AreEqual("UN50AU7700", created.Product.Model);
        Assert.AreEqual("ABC123", created.Product.SerialNumber);
        Assert.AreEqual(EProduct.Tv, created.Product.Type);
        Assert.AreEqual("90 DIAS", created.Guarantee!.ServiceOrderGuarantee);
        Assert.AreEqual(1499.90m, created.PartCost.ServiceOrderPartCost);
        Assert.AreEqual(EServiceOrderStatus.Delivered, created.ServiceOrderStatus);
        Assert.AreEqual(ERepairResult.Repair, created.RepairResult);
        Assert.AreEqual(ERepairStatus.Approved, created.RepairStatus);
        Assert.IsNotNull(created.DeliveryDate);
    }

    [TestMethod]
    public async Task CreateSalesServiceOrder_Valid_ReturnsIdAndPdfAndSendsSaleMessage()
    {
        var result = await _handler.CreateSalesServiceOrder(NewSale());

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(1, result.Data!.Id);
        CollectionAssert.AreEqual(_pdfGenerator.SaleResponse.Data, result.Data.Pdf);
        Assert.AreEqual(1, _pdfGenerator.SaleCalls);
        Assert.AreEqual(0, _pdfGenerator.CheckInCalls);

        var message = _whatsAppQueue.Enqueued.Single() as SendProductSaleMessageRequest;
        Assert.IsNotNull(message);
        Assert.AreEqual("1", message.ServiceOrder);
        Assert.AreEqual("JOÃO DA SILVA", message.CustomerName);
        Assert.AreEqual("41999999999", message.PhoneNumber);
        Assert.AreEqual(1499.90m, message.Amount);
        Assert.AreEqual("90 DIAS", message.Guarantee);
        Assert.AreEqual(EProduct.Tv, message.ProductType);
    }

    [TestMethod]
    public async Task CreateSalesServiceOrder_CreateFails_Returns500WithoutPdf()
    {
        _commandRepository.CreateResponse = new TVS_App.Domain.Shared.BaseResponse<TVS_App.Domain.Entities.ServiceOrder?>(null, 404, "Cliente não encontrado");

        var result = await _handler.CreateSalesServiceOrder(NewSale());

        Assert.AreEqual(500, result.Code);
        Assert.AreEqual("Cliente não encontrado", result.Message);
        Assert.AreEqual(0, _pdfGenerator.SaleCalls);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    [TestMethod]
    public async Task CreateSalesServiceOrder_PdfFails_Returns500AndKeepsOrder()
    {
        _pdfGenerator.SaleResponse = new TVS_App.Domain.Shared.BaseResponse<byte[]>(null, 500, "Falha no comprovante");

        var result = await _handler.CreateSalesServiceOrder(NewSale());

        Assert.AreEqual(500, result.Code);
        Assert.AreEqual("Falha no comprovante", result.Message);
        Assert.IsNotNull(_commandRepository.LastCreated);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    [TestMethod]
    public async Task RegeneratePdf_NotFound_Returns404WithoutPdf()
    {
        _queryRepository.ServiceOrderForUpdate = null;

        var result = await _handler.RegeneratePdf(99);

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
        Assert.AreEqual(0, _pdfGenerator.RegenerateCalls);
    }

    [TestMethod]
    public async Task RegeneratePdf_GeneratorFails_Returns500()
    {
        _queryRepository.ServiceOrderForUpdate = ServiceOrderFactory.Create();
        _pdfGenerator.RegenerateResponse = new TVS_App.Domain.Shared.BaseResponse<byte[]>(null, 500, "Falha no PDF");

        var result = await _handler.RegeneratePdf(1);

        Assert.AreEqual(500, result.Code);
        Assert.AreEqual("Falha no PDF", result.Message);
    }

    [TestMethod]
    public async Task RegeneratePdf_Success_ReturnsPdf()
    {
        _queryRepository.ServiceOrderForUpdate = ServiceOrderFactory.Create();

        var result = await _handler.RegeneratePdf(1);

        Assert.AreEqual(200, result.Code);
        CollectionAssert.AreEqual(_pdfGenerator.RegenerateResponse.Data, result.Data);
        Assert.AreEqual(1, _pdfGenerator.RegenerateCalls);
    }

    private static EditServiceOrderCommand Edit(
        string model = " 43un7300 ",
        string serial = " sn999 ",
        DateTime? entryDate = null,
        decimal partCost = 120m,
        decimal laborCost = 80m,
        EProduct type = EProduct.Tv,
        EEnterprise enterprise = EEnterprise.Cocel,
        string? defect = " sem imagem ",
        string? accessories = " controle ")
        => new(" samsung ", model, serial, defect, accessories, type, " b3 ", enterprise,
            entryDate ?? new DateTime(2026, 10, 1), new DateTime(2026, 10, 2), null, null, null, null,
            " troca do backlight ", " 90 dias ", partCost, laborCost);

    [TestMethod]
    [DataRow("", "SN", 0.0, 0.0, 1, 1, false, "O modelo da ordem de serviço não pode estar vazio")]
    [DataRow("42LB", " ", 0.0, 0.0, 1, 1, false, "O número de série da ordem de serviço não pode estar vazio")]
    [DataRow("42LB", "SN", 0.0, 0.0, 999, 1, false, "Tipo de produto inválido.")]
    [DataRow("42LB", "SN", 0.0, 0.0, 1, 999, false, "Empresa inválida.")]
    [DataRow("42LB", "SN", 0.0, 0.0, 1, 1, true, "A data de entrada é obrigatória.")]
    [DataRow("42LB", "SN", -1.0, 0.0, 1, 1, false, "O custo da peça não pode ser negativo.")]
    [DataRow("42LB", "SN", 0.0, -1.0, 1, 1, false, "O custo de mão de obra não pode ser negativo.")]
    public async Task UpdateServiceOrder_Invalid_Throws(
        string model, string serial, double part, double labor, int type, int enterprise, bool noEntryDate, string message)
    {
        _queryRepository.ServiceOrderForUpdate = ServiceOrderFactory.Create();
        var command = Edit(model, serial, null, (decimal)part, (decimal)labor, (EProduct)type, (EEnterprise)enterprise);
        if (noEntryDate)
            command = command with { EntryDate = null };

        var ex = await Assert.ThrowsExceptionAsync<TVS_App.Application.Exceptions.CommandException<EditServiceOrderCommand>>(
            () => _handler.UpdateServiceOrder(1, command));

        Assert.AreEqual(message, ex.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
    }

    [TestMethod]
    public async Task UpdateServiceOrder_NotFound_Returns404()
    {
        _queryRepository.ServiceOrderForUpdate = null;

        var result = await _handler.UpdateServiceOrder(99, Edit());

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
    }

    [TestMethod]
    public async Task UpdateServiceOrder_Valid_AppliesNormalizedFields()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.UpdateServiceOrder(1, Edit());

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Ordem de serviço alterada com sucesso!", result.Message);
        Assert.AreSame(serviceOrder, _commandRepository.LastUpdated);
        Assert.AreEqual("SAMSUNG", serviceOrder.Product.Brand);
        Assert.AreEqual("43UN7300", serviceOrder.Product.Model);
        Assert.AreEqual("SN999", serviceOrder.Product.SerialNumber);
        Assert.AreEqual("SEM IMAGEM", serviceOrder.Product.Defect);
        Assert.AreEqual("CONTROLE", serviceOrder.Product.Accessories);
        Assert.AreEqual("B3", serviceOrder.Product.Location);
        Assert.AreEqual(EEnterprise.Cocel, serviceOrder.Enterprise);
        Assert.AreEqual(new DateTime(2026, 10, 1), serviceOrder.EntryDate);
        Assert.AreEqual(new DateTime(2026, 10, 2), serviceOrder.InspectionDate);
        Assert.AreEqual("TROCA DO BACKLIGHT", serviceOrder.Solution!.ServiceOrderSolution);
        Assert.AreEqual("90 DIAS", serviceOrder.Guarantee!.ServiceOrderGuarantee);
        Assert.AreEqual(120m, serviceOrder.PartCost.ServiceOrderPartCost);
        Assert.AreEqual(80m, serviceOrder.LaborCost.ServiceOrderLaborCost);
        Assert.AreEqual(200m, result.Data!.TotalAmount);
    }

    [TestMethod]
    public async Task UpdateServiceOrder_DoesNotTouchCustomerStatusOrPurchaseCost()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        serviceOrder.EditServiceOrderStatus(EServiceOrderStatus.Evaluated);
        serviceOrder.EditRepairStatus(ERepairStatus.Waiting);
        serviceOrder.EditRepairResult(ERepairResult.Repair);
        serviceOrder.EditPartPurchaseCost(new TVS_App.Domain.ValueObjects.ServiceOrder.PartPurchaseCost(55m));
        var customer = serviceOrder.Customer;
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        await _handler.UpdateServiceOrder(1, Edit());

        Assert.AreSame(customer, serviceOrder.Customer);
        Assert.AreEqual(7, serviceOrder.CustomerId);
        Assert.AreEqual(EServiceOrderStatus.Evaluated, serviceOrder.ServiceOrderStatus);
        Assert.AreEqual(ERepairStatus.Waiting, serviceOrder.RepairStatus);
        Assert.AreEqual(ERepairResult.Repair, serviceOrder.RepairResult);
        Assert.AreEqual(55m, serviceOrder.PartPurchaseCost.ServiceOrderPartPurchaseCost);
    }

    [TestMethod]
    public async Task UpdateServiceOrder_SaleOrder_KeepsSaleDefectAndNoAccessories()
    {
        var sale = ServiceOrderFactory.Create();
        sale.Product.UpdateProduct("LG", "42LB", "SN", "VENDA", "", EProduct.Tv);
        _queryRepository.ServiceOrderForUpdate = sale;

        await _handler.UpdateServiceOrder(1, Edit(defect: "não liga", accessories: "cabo"));

        Assert.AreEqual("VENDA", sale.Product.Defect);
        Assert.AreEqual("", sale.Product.Accessories);
        Assert.AreEqual("43UN7300", sale.Product.Model);
    }

    private static EditServiceOrderEstimate Estimate(
        EServiceOrderStatus status = EServiceOrderStatus.Repaired,
        ERepairStatus repairStatus = ERepairStatus.Approved,
        ERepairResult? result = ERepairResult.Repair,
        string? message = "Orçamento revisado",
        decimal partPurchaseCost = 75m)
        => new(status, repairStatus, result, message, partPurchaseCost);

    [TestMethod]
    public async Task UpdateServiceOrder_WithEstimate_AppliesStatusResultMessageAndPurchaseCost()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.UpdateServiceOrder(1, Edit() with { Estimate = Estimate() });

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(EServiceOrderStatus.Repaired, serviceOrder.ServiceOrderStatus);
        Assert.AreEqual(ERepairStatus.Approved, serviceOrder.RepairStatus);
        Assert.AreEqual(ERepairResult.Repair, serviceOrder.RepairResult);
        Assert.AreEqual("Orçamento revisado", serviceOrder.EstimateMessage);
        Assert.AreEqual(75m, serviceOrder.PartPurchaseCost.ServiceOrderPartPurchaseCost);
        Assert.AreEqual(EServiceOrderStatus.Repaired, result.Data!.ServiceOrderStatus);
    }

    [TestMethod]
    public async Task UpdateServiceOrder_WithEstimate_CanClearResultAndMessage()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        serviceOrder.EditRepairResult(ERepairResult.Unrepaired);
        serviceOrder.EditEstimateMessage("antiga");
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        await _handler.UpdateServiceOrder(1, Edit() with
        {
            Estimate = Estimate(EServiceOrderStatus.Entered, ERepairStatus.Entered, result: null, message: "  ")
        });

        Assert.AreEqual(EServiceOrderStatus.Entered, serviceOrder.ServiceOrderStatus);
        Assert.AreEqual(ERepairStatus.Entered, serviceOrder.RepairStatus);
        Assert.IsNull(serviceOrder.RepairResult);
        Assert.IsNull(serviceOrder.EstimateMessage);
    }

    [TestMethod]
    [DataRow(999, 1, 1, 0.0, "Situação inválida.")]
    [DataRow(1, 999, 1, 0.0, "Resposta do orçamento inválida.")]
    [DataRow(1, 1, 999, 0.0, "Resultado do orçamento inválido.")]
    [DataRow(1, 1, 1, -1.0, "O custo de compra da peça não pode ser negativo.")]
    public async Task UpdateServiceOrder_WithInvalidEstimate_Throws(int status, int repairStatus, int result, double purchase, string message)
    {
        _queryRepository.ServiceOrderForUpdate = ServiceOrderFactory.Create();
        var command = Edit() with
        {
            Estimate = Estimate((EServiceOrderStatus)status, (ERepairStatus)repairStatus, (ERepairResult)result, partPurchaseCost: (decimal)purchase)
        };

        var ex = await Assert.ThrowsExceptionAsync<TVS_App.Application.Exceptions.CommandException<EditServiceOrderCommand>>(
            () => _handler.UpdateServiceOrder(1, command));

        Assert.AreEqual(message, ex.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
    }

    [TestMethod]
    public async Task UpdateServiceOrder_WithEstimate_DoesNotSendWhatsApp()
    {
        _queryRepository.ServiceOrderForUpdate = ServiceOrderFactory.Create();

        await _handler.UpdateServiceOrder(1, Edit() with { Estimate = Estimate() });

        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
    }

    [TestMethod]
    public async Task UpdatePartPurchaseCost_NotFound_Returns404()
    {
        _queryRepository.ServiceOrderForUpdate = null;

        var result = await _handler.UpdatePartPurchaseCost(99, 10m);

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
    }

    [TestMethod]
    public async Task UpdatePartPurchaseCost_Negative_Throws()
    {
        _queryRepository.ServiceOrderForUpdate = ServiceOrderFactory.Create();

        var ex = await Assert.ThrowsExceptionAsync<TVS_App.Domain.Exceptions.ValueObjectException<TVS_App.Domain.ValueObjects.ServiceOrder.PartPurchaseCost>>(
            () => _handler.UpdatePartPurchaseCost(1, -1m));

        Assert.AreEqual("O valor de compra da peça não pode ser menor que 0", ex.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
    }

    [TestMethod]
    public async Task UpdatePartPurchaseCost_Valid_SavesOnlyThatValue()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        serviceOrder.EditLaborCost(new TVS_App.Domain.ValueObjects.ServiceOrder.LaborCost(300m));
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.UpdatePartPurchaseCost(1, 120.5m);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Valor da peça atualizado.", result.Message);
        Assert.AreSame(serviceOrder, _commandRepository.LastUpdated);
        Assert.AreEqual(120.5m, serviceOrder.PartPurchaseCost.ServiceOrderPartPurchaseCost);
        Assert.AreEqual(300m, serviceOrder.LaborCost.ServiceOrderLaborCost);
        Assert.AreEqual(120.5m, result.Data!.PartPurchaseCost);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task EstimateByCustomer_RightCode_SameEffectAsHome(bool approve)
    {
        var serviceOrder = AwaitingResponse();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;
        var code = " " + serviceOrder.SecurityCode.ToLower() + " ";

        var result = approve
            ? await _handler.ApproveEstimateByCustomer(1, code)
            : await _handler.RejectEstimateByCustomer(1, code);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(approve ? ERepairStatus.Approved : ERepairStatus.Disapproved, serviceOrder.RepairStatus);
        Assert.AreSame(serviceOrder, _commandRepository.LastUpdated);
        Assert.AreEqual(1, _notificationRepository.Created.Count);
        Assert.AreEqual(2, _whatsAppQueue.Enqueued.Count);
    }

    [TestMethod]
    [DataRow(true, "XXXX")]
    [DataRow(false, "XXXX")]
    [DataRow(true, "")]
    public async Task EstimateByCustomer_WrongCode_Returns404WithoutChanges(bool approve, string code)
    {
        var serviceOrder = AwaitingResponse();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = approve
            ? await _handler.ApproveEstimateByCustomer(1, code)
            : await _handler.RejectEstimateByCustomer(1, code);

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
        Assert.AreEqual(ERepairStatus.Waiting, serviceOrder.RepairStatus);
        Assert.IsNull(_commandRepository.LastUpdated);
        Assert.AreEqual(0, _whatsAppQueue.Enqueued.Count);
        Assert.AreEqual(0, _notificationRepository.Created.Count);
    }

    [TestMethod]
    public async Task EstimateByCustomer_UnknownOrder_Returns404()
    {
        _queryRepository.ServiceOrderForUpdate = null;

        var result = await _handler.ApproveEstimateByCustomer(99, "A1B2");

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
    }

    [TestMethod]
    public async Task EstimateByCustomer_AlreadyAnswered_Returns409()
    {
        var serviceOrder = AwaitingResponse();
        serviceOrder.ApproveEstimate();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.RejectEstimateByCustomer(1, serviceOrder.SecurityCode);

        Assert.AreEqual(409, result.Code);
        Assert.AreEqual("Esse orçamento já foi respondido.", result.Message);
    }

    [TestMethod]
    public async Task AddPayment_UnknownOrder_Returns404()
    {
        _queryRepository.ServiceOrderForUpdate = null;

        var result = await _handler.AddPayment(99, new AddPaymentCommand(100m, new DateTime(2026, 11, 5)));

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
    }

    [TestMethod]
    public async Task AddPayment_Valid_AddsAndSaves()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.AddPayment(1, new AddPaymentCommand(150m, new DateTime(2026, 11, 5)));

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Pagamento adicionado.", result.Message);
        Assert.AreEqual(1, serviceOrder.Payments.Count);
        Assert.AreEqual(150m, serviceOrder.Payments[0].Amount);
        Assert.AreEqual(1, serviceOrder.Payments[0].ServiceOrderId);
        Assert.AreSame(serviceOrder, _commandRepository.LastUpdated);
    }

    private static ServiceOrder WithPayment(long paymentId, decimal amount = 100m)
    {
        var serviceOrder = ServiceOrderFactory.Create();
        serviceOrder.AddPayment(new TVS_App.Domain.Entities.Payment(amount, new DateTime(2026, 11, 1), 1) { Id = paymentId });
        return serviceOrder;
    }

    [TestMethod]
    public async Task UpdatePayment_Valid_UpdatesAndSaves()
    {
        var serviceOrder = WithPayment(5);
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.UpdatePayment(1, 5, new UpdatePaymentCommand(80m, new DateTime(2026, 11, 3)));

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Pagamento atualizado.", result.Message);
        Assert.AreEqual(80m, serviceOrder.Payments[0].Amount);
        Assert.AreEqual(new DateTime(2026, 11, 3), serviceOrder.Payments[0].PaidAt);
    }

    [TestMethod]
    public async Task UpdatePayment_UnknownPayment_Returns404()
    {
        _queryRepository.ServiceOrderForUpdate = WithPayment(5);

        var result = await _handler.UpdatePayment(1, 6, new UpdatePaymentCommand(80m, DateTime.Today));

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Pagamento não encontrado", result.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
    }

    [TestMethod]
    public async Task RemovePayment_Valid_RemovesAndSaves()
    {
        var serviceOrder = WithPayment(5);
        _queryRepository.ServiceOrderForUpdate = serviceOrder;

        var result = await _handler.RemovePayment(1, 5);

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Pagamento removido.", result.Message);
        Assert.AreEqual(0, serviceOrder.Payments.Count);
    }

    [TestMethod]
    public async Task RemovePayment_UnknownPayment_Returns404()
    {
        _queryRepository.ServiceOrderForUpdate = WithPayment(5);

        var result = await _handler.RemovePayment(1, 6);

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Pagamento não encontrado", result.Message);
    }

    [TestMethod]
    [DataRow(0.0, "O valor deve ser maior que zero")]
    [DataRow(-5.0, "O valor deve ser maior que zero")]
    public async Task AddPayment_InvalidAmount_Throws(double amount, string message)
    {
        _queryRepository.ServiceOrderForUpdate = ServiceOrderFactory.Create();

        var ex = await Assert.ThrowsExceptionAsync<TVS_App.Application.Exceptions.CommandException<AddPaymentCommand>>(
            () => _handler.AddPayment(1, new AddPaymentCommand((decimal)amount, DateTime.Today)));

        Assert.AreEqual(message, ex.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
    }

    [TestMethod]
    public async Task AddPayment_NoDate_Throws()
    {
        _queryRepository.ServiceOrderForUpdate = ServiceOrderFactory.Create();

        var ex = await Assert.ThrowsExceptionAsync<TVS_App.Application.Exceptions.CommandException<AddPaymentCommand>>(
            () => _handler.AddPayment(1, new AddPaymentCommand(10m, default)));

        Assert.AreEqual("Informe a data do pagamento.", ex.Message);
    }

    [TestMethod]
    public async Task UpdatePayment_InvalidValues_Throw()
    {
        _queryRepository.ServiceOrderForUpdate = WithPayment(5);

        var zero = await Assert.ThrowsExceptionAsync<TVS_App.Application.Exceptions.CommandException<UpdatePaymentCommand>>(
            () => _handler.UpdatePayment(1, 5, new UpdatePaymentCommand(0m, DateTime.Today)));
        var noDate = await Assert.ThrowsExceptionAsync<TVS_App.Application.Exceptions.CommandException<UpdatePaymentCommand>>(
            () => _handler.UpdatePayment(1, 5, new UpdatePaymentCommand(10m, default)));

        Assert.AreEqual("O valor deve ser maior que zero", zero.Message);
        Assert.AreEqual("Informe a data do pagamento.", noDate.Message);
        Assert.IsNull(_commandRepository.LastUpdated);
    }

    [TestMethod]
    public async Task GetPaymentReceipt_UnknownOrder_Returns404()
    {
        _queryRepository.ServiceOrderForUpdate = null;

        var result = await _handler.GetPaymentReceipt(99, 1);

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Essa ordem de serviço não existe", result.Message);
        Assert.AreEqual(0, _pdfGenerator.ReceiptCalls);
    }

    [TestMethod]
    public async Task GetPaymentReceipt_UnknownPayment_Returns404()
    {
        _queryRepository.ServiceOrderForUpdate = WithPayment(5);

        var result = await _handler.GetPaymentReceipt(1, 6);

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Pagamento não encontrado", result.Message);
        Assert.AreEqual(0, _pdfGenerator.ReceiptCalls);
    }

    [TestMethod]
    public async Task GetPaymentReceipt_GeneratorFails_Returns500()
    {
        _queryRepository.ServiceOrderForUpdate = WithPayment(5);
        _pdfGenerator.ReceiptResponse = new TVS_App.Domain.Shared.BaseResponse<byte[]>(null, 500, "Falha no comprovante");

        var result = await _handler.GetPaymentReceipt(1, 5);

        Assert.AreEqual(500, result.Code);
        Assert.AreEqual("Falha no comprovante", result.Message);
    }

    [TestMethod]
    public async Task GetPaymentReceipt_Success_ReturnsPdf()
    {
        _queryRepository.ServiceOrderForUpdate = WithPayment(5);

        var result = await _handler.GetPaymentReceipt(1, 5);

        Assert.AreEqual(200, result.Code);
        CollectionAssert.AreEqual(_pdfGenerator.ReceiptResponse.Data, result.Data);
        Assert.AreEqual(1, _pdfGenerator.ReceiptCalls);
        Assert.AreEqual(5, _pdfGenerator.LastReceiptPaymentId);
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
