using TVS_App.Application.Exceptions;
using TVS_App.Application.Handlers.Customers;
using TVS_App.Application.Requests.CustomerRequests.CustomerCommands;
using TVS_App.Domain.Exceptions;
using TVS_App.Domain.ValueObjects.Customer;
using TVSApp.Tests.Handlers.Fakes;

namespace TVSApp.Tests.Handlers;

[TestClass]
public class CustomerHandlerTests
{
    private FakeCustomerQueryRepository _queryRepository = null!;
    private FakeCustomerCommandRepository _commandRepository = null!;
    private CustomerQueryHandler _queryHandler = null!;
    private CustomerCommandHandler _commandHandler = null!;

    [TestInitialize]
    public void Setup()
    {
        _queryRepository = new FakeCustomerQueryRepository();
        _commandRepository = new FakeCustomerCommandRepository();
        _queryHandler = new CustomerQueryHandler(_queryRepository);
        _commandHandler = new CustomerCommandHandler(_commandRepository, _queryRepository);
    }

    private static CreateCustomerCommand NewCustomer(string name = "maria souza", string phone = "41999990000", string? document = null)
        => new(name, "Rua A", "Centro", "Curitiba", "10", "80000000", "pr", phone, "", "", document);

    private static UpdateCustomerCommand ExistingCustomer(long id, string phone)
        => new(id, "João da Silva", "Rua A", "Centro", "Curitiba", "10", "80000000", "PR", phone, "", "", null);

    [TestMethod]
    public async Task SearchByName_LessThanThreeLetters_Returns400()
    {
        var result = await _queryHandler.SearchByName("jo");

        Assert.AreEqual(400, result.Code);
        Assert.AreEqual("Digite pelo menos 3 letras.", result.Message);
        Assert.IsNull(_queryRepository.LastLimit);
    }

    [TestMethod]
    public async Task SearchByName_ValidTerm_AsksFor21AndMapsCustomers()
    {
        _queryRepository.Customers.Add(ServiceOrderFactory.Create().Customer);

        var result = await _queryHandler.SearchByName("joão");

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(21, _queryRepository.LastLimit);
        Assert.AreEqual(1, result.Data!.Count);
        Assert.AreEqual("JOÃO DA SILVA", result.Data[0].Name);
    }

    [TestMethod]
    public async Task Create_WithoutName_Throws()
    {
        var ex = await Assert.ThrowsExceptionAsync<CommandException<CreateCustomerCommand>>(
            () => _commandHandler.Create(NewCustomer(name: " ")));

        Assert.AreEqual("O nome do cliente não pode estar vazio", ex.Message);
    }

    [TestMethod]
    public async Task Create_WithoutPhone_Throws()
    {
        var ex = await Assert.ThrowsExceptionAsync<CommandException<CreateCustomerCommand>>(
            () => _commandHandler.Create(NewCustomer(phone: "")));

        Assert.AreEqual("O telefone do cliente não pode estar vazio", ex.Message);
    }

    [TestMethod]
    public async Task Create_WithoutDocument_CreatesCustomerWithoutDocument()
    {
        var result = await _commandHandler.Create(NewCustomer(document: null));

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(10, result.Data!.Id);
        Assert.AreEqual("MARIA SOUZA", result.Data.Name);
        Assert.IsNull(result.Data.Document);
        Assert.IsNull(_commandRepository.Created!.Document);
    }

    [TestMethod]
    public async Task Create_WithAlphanumericCnpj_StoresNormalized()
    {
        var result = await _commandHandler.Create(NewCustomer(document: "12.abc.345/01de-35"));

        Assert.AreEqual("12ABC34501DE35", result.Data!.Document);
    }

    [TestMethod]
    public async Task Create_WithInvalidDocument_Throws()
    {
        var ex = await Assert.ThrowsExceptionAsync<ValueObjectException<Document>>(
            () => _commandHandler.Create(NewCustomer(document: "123")));

        Assert.AreEqual(Document.InvalidMessage, ex.Message);
        Assert.IsNull(_commandRepository.Created);
    }

    [TestMethod]
    public async Task Update_NotFound_Returns404()
    {
        var result = await _commandHandler.Update(99, ExistingCustomer(0, "41911112222"));

        Assert.AreEqual(404, result.Code);
        Assert.AreEqual("Cliente não encontrado.", result.Message);
        Assert.IsNull(_commandRepository.Updated);
    }

    [TestMethod]
    public async Task Update_Existing_UsesRouteIdAndUpdatesPhone()
    {
        var customer = ServiceOrderFactory.Create().Customer;
        _queryRepository.Customers.Add(customer);

        var result = await _commandHandler.Update(customer.Id, ExistingCustomer(0, "41911112222"));

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual("Cliente atualizado com sucesso", result.Message);
        Assert.AreEqual("41911112222", customer.Phone.CustomerPhone);
        Assert.AreSame(customer, _commandRepository.Updated);
    }
}
