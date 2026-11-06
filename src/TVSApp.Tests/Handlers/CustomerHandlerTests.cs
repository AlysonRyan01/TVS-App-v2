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

[TestClass]
public class CustomerSearchHandlerTests
{
    private FakeCustomerQueryRepository _repository = null!;
    private CustomerQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeCustomerQueryRepository();
        _handler = new CustomerQueryHandler(_repository);
    }

    private static TVS_App.Application.Requests.CustomerRequests.CustomerQueries.CustomerSearchQuery Query(
        string? name = null, string? phone = null, string? document = null, int page = 1, int size = 20)
        => new(name, phone, document, page, size);

    [TestMethod]
    public async Task NoFilters_PassesEmptyFilterAndDefaultPage()
    {
        _repository.Customers.Add(ServiceOrderFactory.Create().Customer);

        var result = await _handler.Search(Query(name: "  ", phone: "", document: null));

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(new TVS_App.Domain.Repositories.Customers.CustomerSearchFilter(null, null, null), _repository.LastSearchFilter);
        Assert.AreEqual(1, _repository.LastSearchPage);
        Assert.AreEqual(20, _repository.LastSearchPageSize);
        Assert.AreEqual(1, result.Data!.TotalCount);
        Assert.AreEqual(1, result.Data.Items.Count());
        Assert.AreEqual("1 cliente encontrado.", result.Message);
    }

    [TestMethod]
    public async Task Empty_ReturnsFriendlyMessage()
    {
        var result = await _handler.Search(Query());

        Assert.AreEqual(200, result.Code);
        Assert.AreEqual(0, result.Data!.TotalCount);
        Assert.AreEqual("Nenhum cliente encontrado.", result.Message);
    }

    [TestMethod]
    public async Task ManyResults_PluralMessage()
    {
        var a = ServiceOrderFactory.Create().Customer; a.Id = 1;
        var b = ServiceOrderFactory.Create().Customer; b.Id = 2;
        _repository.Customers.AddRange([a, b]);

        var result = await _handler.Search(Query());

        Assert.AreEqual("2 clientes encontrados.", result.Message);
    }

    [TestMethod]
    public async Task Name_IsTrimmed()
    {
        await _handler.Search(Query(name: "  maria "));

        Assert.AreEqual("maria", _repository.LastSearchFilter!.Name);
    }

    [TestMethod]
    [DataRow("ma")]
    [DataRow(" j ")]
    public async Task Name_ShorterThanThree_Returns400(string name)
    {
        var result = await _handler.Search(Query(name: name));

        Assert.AreEqual(400, result.Code);
        Assert.AreEqual("Digite pelo menos 3 letras do nome.", result.Message);
        Assert.IsNull(_repository.LastSearchFilter);
    }

    [TestMethod]
    [DataRow("(41) 99999-0000", "41999990000")]
    [DataRow("+55 41 99999.0000", "5541999990000")]
    [DataRow("9999", "9999")]
    public async Task Phone_KeepsOnlyDigits(string phone, string digits)
    {
        await _handler.Search(Query(phone: phone));

        Assert.AreEqual(digits, _repository.LastSearchFilter!.PhoneDigits);
    }

    [TestMethod]
    [DataRow("123")]
    [DataRow("(41)")]
    [DataRow("abc")]
    public async Task Phone_WithLessThanFourDigits_Returns400(string phone)
    {
        var result = await _handler.Search(Query(phone: phone));

        Assert.AreEqual(400, result.Code);
        Assert.AreEqual("Digite pelo menos 4 números do telefone.", result.Message);
    }

    [TestMethod]
    [DataRow("123.456.789-09", "12345678909")]
    [DataRow("12.abc.345/0001-95", "12ABC3450001")]
    public async Task Document_IsNormalized(string document, string normalized)
    {
        await _handler.Search(Query(document: document));

        StringAssert.StartsWith(_repository.LastSearchFilter!.DocumentDigits, normalized[..Math.Min(normalized.Length, 12)]);
    }

    [TestMethod]
    public async Task Document_ShorterThanThree_Returns400()
    {
        var result = await _handler.Search(Query(document: "1.2"));

        Assert.AreEqual(400, result.Code);
        Assert.AreEqual("Digite pelo menos 3 caracteres do CPF/CNPJ.", result.Message);
    }

    [TestMethod]
    public async Task Filters_AreCombined()
    {
        await _handler.Search(Query(name: "maria", phone: "4199", document: "123"));

        Assert.AreEqual(new TVS_App.Domain.Repositories.Customers.CustomerSearchFilter("maria", "4199", "123"), _repository.LastSearchFilter);
    }

    [TestMethod]
    [DataRow(0, 20)]
    [DataRow(1, 0)]
    [DataRow(1, 101)]
    public async Task InvalidPaging_Returns400(int page, int size)
    {
        var result = await _handler.Search(Query(page: page, size: size));

        Assert.AreEqual(400, result.Code);
        Assert.AreEqual("Paginação inválida.", result.Message);
    }

    [TestMethod]
    public async Task Paging_IsPassedThrough()
    {
        await _handler.Search(Query(page: 3, size: 50));

        Assert.AreEqual(3, _repository.LastSearchPage);
        Assert.AreEqual(50, _repository.LastSearchPageSize);
    }

    [TestMethod]
    public async Task Result_IsMappedToDto()
    {
        var customer = ServiceOrderFactory.Create().Customer;
        _repository.Customers.Add(customer);

        var result = await _handler.Search(Query());

        var dto = result.Data!.Items.Single();
        Assert.AreEqual(customer.Id, dto.Id);
        Assert.AreEqual(customer.Name.CustomerName, dto.Name);
        Assert.AreEqual(customer.Phone.CustomerPhone, dto.Phone);
    }
}
