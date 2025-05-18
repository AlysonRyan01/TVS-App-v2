using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Exceptions;
using TVS_App.Domain.ValueObjects.Customer;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVS_App.Tests.Entities;

[TestClass]
public class ServiceOrderTests
{
    private Product _product;
    private Customer _customer;

    public ServiceOrderTests()
    {
        _product = new Product(
            "Samsung",
            "UN40J5200AG",
            "ABC123",
            "Não liga",
            "CABO",
            EProduct.Tv
        );

        _customer = new Customer(
            new Name("Alyson Ryan Ullirsch"),
            new Address("Rua Teste", "Centro", "Cidade", "123", "12345678", "Estado"),
            new Phone("41999999999"),
            new Phone("41999999999"),
            new Email("teste@email.com")
        );
    }

    [TestMethod]
    public void deve_criar_ordem_de_servico_com_status_inicial()
    {
        var os = new ServiceOrder(EEnterprise.Particular, _customer.Id, _product);

        Assert.AreEqual(EEnterprise.Particular, os.Enterprise);
        Assert.AreEqual(ERepairStatus.Entered, os.RepairStatus);
        Assert.AreEqual(EServiceOrderStatus.Entered, os.ServiceOrderStatus);
        Assert.AreEqual(0, os.TotalAmount);
        Assert.IsNull(os.Solution);
    }

    [TestMethod]
    public void deve_adicionar_orcamento()
    {
        var os = new ServiceOrder(EEnterprise.Particular, _customer.Id, _product);

        os.AddEstimate("Trocar a placa", "3 meses", 150, 200, ERepairResult.Repair, "");

        Assert.AreEqual("Trocar a placa", os.Solution!.ServiceOrderSolution);
        Assert.AreEqual(150, os.PartCost.ServiceOrderPartCost);
        Assert.AreEqual(200, os.LaborCost.ServiceOrderLaborCost);
        Assert.AreEqual(350, os.TotalAmount);
        Assert.AreEqual(ERepairStatus.Waiting, os.RepairStatus);
        Assert.AreEqual(EServiceOrderStatus.Evaluated, os.ServiceOrderStatus);
    }

    [TestMethod]
    public void deve_aprovar_orcamento()
    {
        var os = new ServiceOrder(EEnterprise.Particular, _customer.Id, _product);
        os.AddEstimate("Trocar a placa", "3 meses",  150, 200, ERepairResult.Repair, "");

        os.ApproveEstimate();

        Assert.AreEqual(ERepairStatus.Approved, os.RepairStatus);
    }

    [TestMethod]
    [ExpectedException(typeof(EntityException<ServiceOrder>))]
    public void deve_lancar_excecao_ao_aprovar_sem_solucao()
    {
        var os = new ServiceOrder(EEnterprise.Particular, _customer.Id, _product);
        os.ApproveEstimate();
    }

    [TestMethod]
    public void deve_rejeitar_orcamento()
    {
        var os = new ServiceOrder(EEnterprise.Particular, _customer.Id, _product);
        os.AddEstimate("Não compensa o conserto","3 meses", 100, 100, ERepairResult.Unrepaired, "");

        os.RejectEstimate();

        Assert.AreEqual(ERepairStatus.Disapproved, os.RepairStatus);
    }

    [TestMethod]
    public void deve_executar_conserto_apos_aprovacao()
    {
        var os = new ServiceOrder(EEnterprise.Particular, _customer.Id, _product);
        os.AddEstimate("Trocar fonte","3 meses", 100, 150, ERepairResult.Repair, "");
        os.ApproveEstimate();

        os.ExecuteRepair();

        Assert.AreEqual(EServiceOrderStatus.Repaired, os.ServiceOrderStatus);
        Assert.IsNotNull(os.RepairDate);
    }

    [TestMethod]
    [ExpectedException(typeof(EntityException<ServiceOrder>))]
    public void nao_deve_executar_conserto_se_nao_aprovado()
    {
        var os = new ServiceOrder(EEnterprise.Particular, _customer.Id, _product);
        os.ExecuteRepair();
    }

    [TestMethod]
    public void deve_registrar_entrega()
    {
        var os = new ServiceOrder(EEnterprise.Particular, _customer.Id, _product);
        os.AddEstimate("Reparo simples","3 meses", 50, 50, ERepairResult.Repair, "");
        os.ApproveEstimate();
        os.ExecuteRepair();

        os.AddDelivery();

        Assert.AreEqual(EServiceOrderStatus.Delivered, os.ServiceOrderStatus);
        Assert.IsNotNull(os.DeliveryDate);
    }

    [TestMethod]
    public void deve_atualizar_ordem_de_servico()
    {
        var os = new ServiceOrder(EEnterprise.Particular, _customer.Id, _product);

        os.UpdateServiceOrder(_customer, "LG", "Novo modelo", "ZZZ999", "Novo defeito", "Sem acessórios", EProduct.Tv, EEnterprise.Cocel);

        Assert.AreEqual("LG", os.Product.Brand);
        Assert.AreEqual("Novo modelo", os.Product.Model);
        Assert.AreEqual("ZZZ999", os.Product.SerialNumber);
        Assert.AreEqual("Novo defeito", os.Product.Defect);
        Assert.AreEqual("Sem acessórios", os.Product.Accessories);
        Assert.AreEqual(EEnterprise.Cocel, os.Enterprise);
    }
}