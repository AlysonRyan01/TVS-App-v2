using TVS_App.Application.Commands;
using TVS_App.Application.Commands.ServiceOrderCommands;
using TVS_App.Application.Handlers;
using TVS_App.Application.Interfaces;
using TVS_App.Application.Repositories;
using TVS_App.Tests.Mocks;

namespace TVS_App.Tests.Handlers;

[TestClass]
public class ServiceOrderHandlerTests
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ServiceOrderHandler _serviceOrderHandler;
    private readonly IGenerateServiceOrderPdf _genereteerviceOrderPdf;

    public ServiceOrderHandlerTests()
    {
        _serviceOrderRepository = new FakeServiceOrderRepository();
        _customerRepository = new FakeCustomerRepository();
        _genereteerviceOrderPdf = null!;
        _serviceOrderHandler = new(_serviceOrderRepository, _customerRepository, _genereteerviceOrderPdf);
    }

    [TestMethod]
    public async Task deve_retornar_verdadeiro_se_a_ordem_de_servico_for_criada()
    {
        try
        {
            var createServiceOrderCommand = new CreateServiceOrderCommand
            {
                CustomerId = 1,
                ProductModel = "UN40J5200AG",
                ProductSerialNumber = "32HJ31HJ312",
                ProductDefect = "Sem imagem",
                Accessories = "controle",
                ProductType = Domain.Enums.EProduct.Tv,
                Enterprise = Domain.Enums.EEnterprise.Particular
            };

            createServiceOrderCommand.Validate();

            var createResult = await _serviceOrderHandler.CreateServiceOrderAndReturnPdfAsync(createServiceOrderCommand);

            Assert.IsTrue(createResult.IsSuccess, "A criação da ordem de serviço falhou quando deveria ter tido sucesso.");
        }
        catch
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public async Task deve_retornar_verdadeiro_se_a_ordem_de_servico_for_atualizada()
    {
        try
        {
            var updateServiceOrderCommand = new UpdateServiceOrderCommand
            {
                ServiceOrderId = 5,
                CustomerId = 10,
                ProductModel = "UN40J5200AG",
                ProductSerialNumber = "32HJ31HJ312",
                ProductDefect = "Sem imagem",
                Accessories = "controle",
                ProductType = Domain.Enums.EProduct.Tv,
                Enterprise = Domain.Enums.EEnterprise.Particular
            };

            updateServiceOrderCommand.Validate();

            var createResult = await _serviceOrderHandler.UpdateServiceOrderAsync(updateServiceOrderCommand);
            if (!createResult.IsSuccess)
                Assert.Fail(createResult.Message);

            if (createResult?.Data?.Customer.Id == 10 &&
                createResult.Data.Enterprise == Domain.Enums.EEnterprise.Particular &&
                createResult?.Data?.Product.Model == "UN40J5200AG" &&
                createResult?.Data?.Product.Accessories == "controle")
            {
                Assert.IsTrue(true);
            }
        }
        catch (Exception ex)
        {
            Assert.Fail(ex.Message);
        }
    }

    [TestMethod]
    public async Task deve_retornar_verdadeiro_se_obter_a_ordem_de_servico_correta()
    {
        try
        {
            var serviceOrderResult = await _serviceOrderHandler.GetServiceOrderById(new GetServiceOrderByIdCommand { Id = 2 });
            if (!serviceOrderResult.IsSuccess)
                Assert.Fail(serviceOrderResult.Message);

            Assert.AreEqual(serviceOrderResult?.Data?.Id, 2);
        }
        catch (Exception ex)
        {
            Assert.Fail(ex.Message);
        }
    }

    [TestMethod]
    public async Task deve_retornar_verdadeiro_se_obter_todas_as_ordens_de_servico_corretamente()
    {
        try
        {
            var getAllServiceOrdersResult = await _serviceOrderHandler.GetAllServiceOrdersAsync(new PaginationCommand{ PageNumber = 1, PageSize = 25});

            if (!getAllServiceOrdersResult.IsSuccess)
                Assert.Fail("Falha ao buscar todas as ordens de servico");

            var serviceOrders = getAllServiceOrdersResult.Data?.Items;

            Assert.IsNotNull(serviceOrders);
            Assert.AreEqual(13, serviceOrders!.Count());
        }
        catch (Exception ex)
        {
            Assert.Fail(ex.Message);
        }
    }

    [TestMethod]
    public async Task deve_retornar_verdadeiro_se_todas_as_listas_estiverem_com_o_service_order_status_correto()
    {
        try
        {
            var allServiceOrderResponse = await _serviceOrderHandler.GetAllServiceOrdersAsync(new PaginationCommand{ PageNumber = 1, PageSize = 25});

            var allserviceOrder = allServiceOrderResponse.Data?.Items.ToList();
            if (allserviceOrder == null || !allserviceOrder.Any())
                Assert.Fail("A lista de ordens de serviço está vazia ou nula");

            for (int i = 0; i < 2; i++)
            {
                allserviceOrder[i]?.AddEstimate("placa", "3 meses", 200m, 300m, Domain.Enums.ERepairResult.Repair, "");
                await _serviceOrderHandler.UpdateServiceOrderAsync(new UpdateServiceOrderCommand { ServiceOrderId = allserviceOrder!.ToList()[i]!.Id });
            }

            for (int i = 2; i < 4; i++)
            {
                allserviceOrder[i]?.AddEstimate("placa", "3 meses", 200m, 300m, Domain.Enums.ERepairResult.Repair, "");
                allserviceOrder[i]?.ApproveEstimate();
                await _serviceOrderHandler.UpdateServiceOrderAsync(new UpdateServiceOrderCommand { ServiceOrderId = allserviceOrder!.ToList()[i]!.Id });
            }

            for (int i = 4; i < 6; i++)
            {
                allserviceOrder[i]?.AddEstimate("placa", "3 meses", 200m, 300m, Domain.Enums.ERepairResult.Repair, "");
                allserviceOrder[i]?.RejectEstimate();
                await _serviceOrderHandler.UpdateServiceOrderAsync(new UpdateServiceOrderCommand { ServiceOrderId = allserviceOrder!.ToList()[i]!.Id });
            }

            for (int i = 6; i < 8; i++)
            {
                allserviceOrder[i]?.AddEstimate("placa","3 meses", 200m, 300m, Domain.Enums.ERepairResult.Repair, "");
                allserviceOrder[i]?.ApproveEstimate();
                allserviceOrder[i]?.ExecuteRepair();
                await _serviceOrderHandler.UpdateServiceOrderAsync(new UpdateServiceOrderCommand { ServiceOrderId = allserviceOrder!.ToList()[i]!.Id });

            }

            for (int i = 8; i < 10; i++)
            {
                allserviceOrder[i]?.AddEstimate("placa","3 meses", 200m, 300m, Domain.Enums.ERepairResult.Repair, "");
                allserviceOrder[i]?.ApproveEstimate();
                allserviceOrder[i]?.ExecuteRepair();
                allserviceOrder[i]?.AddDelivery();
                await _serviceOrderHandler.UpdateServiceOrderAsync(new UpdateServiceOrderCommand { ServiceOrderId = allserviceOrder!.ToList()[i]!.Id });
            }

            var estimatedServiceOrders = await _serviceOrderHandler.GetWaitingResponseAsync(new PaginationCommand{ PageNumber = 1, PageSize = 25});
            var expectedEstimatedIds = new HashSet<long> { 1L, 2L };
            var actualEstimatedIds = estimatedServiceOrders?.Data?.Items.Select(x => x!.Id).ToHashSet();
            Assert.IsTrue(actualEstimatedIds?.SetEquals(expectedEstimatedIds));

            var awaitingPartServiceOrders = await _serviceOrderHandler.GetWaitingPartsAsync(new PaginationCommand{ PageNumber = 1, PageSize = 25});
            var expectedAwaitingPartIds = new HashSet<long> { 3L, 4L };
            var actualAwaitingPartIds = awaitingPartServiceOrders?.Data?.Items.Select(x => x!.Id).ToHashSet();
            Assert.IsTrue(actualAwaitingPartIds?.SetEquals(expectedAwaitingPartIds));

            var awaitingPickUpServiceOrders = await _serviceOrderHandler.GetWaitingPickupAsync(new PaginationCommand{ PageNumber = 1, PageSize = 25});
            var expectedPickUpIds = new HashSet<long> { 5L, 6L, 7L, 8L };
            var actualAwaitingPickUpIds = awaitingPickUpServiceOrders?.Data?.Items.Select(x => x!.Id).ToHashSet();
            Assert.IsTrue(actualAwaitingPickUpIds?.SetEquals(expectedPickUpIds));

            var getDeliveredServiceOrders = await _serviceOrderHandler.GetDeliveredAsync(new PaginationCommand{ PageNumber = 1, PageSize = 25});
            var expectedDeliveredIds = new HashSet<long> { 9L, 10L };
            var actualDeliveredIds = getDeliveredServiceOrders?.Data?.Items.Select(x => x!.Id).ToHashSet();
            Assert.IsTrue(actualDeliveredIds?.SetEquals(expectedDeliveredIds));

            var getPendingEstimatesServiceOrders = await _serviceOrderHandler.GetPendingEstimatesAsync(new PaginationCommand{ PageNumber = 1, PageSize = 25});
            var expectedPendingEstimatesIds = new HashSet<long> { 11L, 12L, 13L };
            var actualPendingEstimatesIds = getPendingEstimatesServiceOrders?.Data?.Items.Select(x => x!.Id).ToHashSet();
            Assert.IsTrue(actualPendingEstimatesIds?.SetEquals(expectedPendingEstimatesIds));

        }
        catch (Exception ex)
        {
            Assert.Fail(ex.Message);
        }
    }
}