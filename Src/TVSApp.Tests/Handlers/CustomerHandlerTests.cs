using TVS_App.Application.Commands;
using TVS_App.Application.Commands.CustomerCommands;
using TVS_App.Application.Handlers;
using TVS_App.Application.Repositories;
using TVS_App.Domain.Entities;
using TVS_App.Domain.ValueObjects.Customer;
using TVS_App.Tests.Mocks;

namespace TVS_App.Tests.Handlers;

[TestClass]
public class CustomerHandlerTests
{
    private readonly CustomerHandler _customerHandler;
    private readonly ICustomerRepository _fakeCustomerRepo;
    private readonly Customer _customer;
    private readonly CreateCustomerCommand _createCustomerCommand;
    private readonly GetCustomerByIdCommand _getCustomerByIdCommand;
    private readonly UpdateCustomerCommand _updateCustomerCommand;

    public CustomerHandlerTests()
    {
        _fakeCustomerRepo = new FakeCustomerRepository();
        _customerHandler = new CustomerHandler(_fakeCustomerRepo);

        _customer = new Customer(
            new Name("Alyson"),
            new Address("Rua Centenario", "Centro", "Campo Largo", "123", "83601000", "Parana"),
            new Phone("41997561468"),
            new Phone("41997561468"),
            new Email("alysonullirsch8@gmail.com"));

        _createCustomerCommand = new()
        {
            Name = "Alyson Ryan Ullirsch",
            Street = "Rua Centenario",
            City = "Centro",
            Number = "123",
            State = "Parana",
            Phone = "41997561468",
            Email = "alysonullirsch8@gmail.com"
        };

        _getCustomerByIdCommand = new GetCustomerByIdCommand
        {
            Id = 12
        };

        _updateCustomerCommand = new UpdateCustomerCommand
        {
            Id = 1,
            Name = "Marcos da Silva",
            Street = "Rua do Marcos",
            City = "Cidade do Marcos",
            Number = "Numero do Marcos",
            Neighborhood = "Bairro do Marcos",
            ZipCode = "83920300",
            State = "Estado do Marcos",
            Email = "marcos@gmail.com",
            Phone = "4132923047"
        };
    }

    [TestMethod]
    public async Task deve_adicionar_e_retornar_um_cliente()
    {
        try
        {
            var createResult = await _customerHandler.CreateCustomerAsync(_createCustomerCommand);
            if (!createResult.IsSuccess)
                Assert.Fail(createResult.Message);

            var customer = createResult.Data;

            var createResult2 = await _customerHandler.GetCustomerByIdAsync(_getCustomerByIdCommand);
            if (!createResult2.IsSuccess)
                Assert.Fail(createResult2.Message);

            var customer2 = createResult2.Data;

            Assert.AreEqual(customer, customer2);
        }
        catch (Exception ex)
        {
            Assert.Fail(ex.Message);
        }
    }

    [TestMethod]
    public async Task deve_retornar_verdade_se_atualizar_o_usuario()
    {
        try
        {
            var createCustomerResult = await _customerHandler.CreateCustomerAsync(_createCustomerCommand);
            if (!createCustomerResult.IsSuccess)
                Assert.Fail();

            var customer = createCustomerResult.Data;

            var updateCustomerResult = await _customerHandler.UpdateCustomerAsync(_updateCustomerCommand);
            if (!updateCustomerResult.IsSuccess)
                Assert.Fail();

            if (updateCustomerResult!.Data!.Name.CustomerName == "Marcos da Silva" &&
                updateCustomerResult.Data!.Address.Street == "Rua do Marcos" &&
                updateCustomerResult.Data!.Address.Number == "Numero do Marcos" &&
                updateCustomerResult.Data!.Address.City == "Cidade do Marcos" &&
                updateCustomerResult.Data!.Address.Neighborhood == "Bairro do Marcos" &&
                updateCustomerResult.Data!.Address.ZipCode == "83920300" &&
                updateCustomerResult.Data!.Phone.CustomerPhone == "4132923047" &&
                updateCustomerResult.Data!.Email!.CustomerEmail == "marcos@gmail.com")
                Assert.IsTrue(true);
        }
        catch
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public async Task deve_retornar_verdade_se_retornar_todos_os_clientes()
    {
        try
        {
            for (int i = 1; i <= 30; i++)
            {
                var command = new CreateCustomerCommand
                {
                    Name = $"Cliente {i}",
                    Street = $"Rua {i}",
                    City = $"Cidade {i}",
                    Number = $"{i}",
                    Neighborhood = $"Bairro {i}",
                    ZipCode = $"836000{i:D2}",
                    State = "PR",
                    Phone = $"41999999{i:D4}",
                    Email = $"cliente{i}@teste.com"
                };

                var createResult = await _customerHandler.CreateCustomerAsync(command);
                if (!createResult.IsSuccess)
                    Assert.Fail($"Falha ao criar cliente {i}");
            }

            var getAllCustomersResult = await _customerHandler.GetAllCustomersAsync(new PaginationCommand{ PageNumber = 1, PageSize = 25});
            
            if (!getAllCustomersResult.IsSuccess)
                Assert.Fail("Falha ao buscar todos os clientes");

            var clientes = getAllCustomersResult.Data?.Items;

            Assert.IsNotNull(clientes);
            Assert.AreEqual(25, clientes!.Count());
        }
        catch
        {
            Assert.Fail();
        }
    }
}    


