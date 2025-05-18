using TVS_App.Domain.Entities;
using TVS_App.Domain.Exceptions;
using TVS_App.Domain.ValueObjects.Customer;

namespace TVS_App.Tests.Entities;

[TestClass]
public class CustomerTests
{
    private readonly Name _name;
    private readonly Address _address;
    private readonly Phone _phone;
    private readonly Phone _phone2;
    private readonly Email _email;

    public CustomerTests()
    {
        _name = new Name("Alyson Ryan Ullirsch");
        _address = new Address("Rua Centenario", "Centro", "Campo Largo", "123", " 83601000", "Parana");
        _phone = new Phone("41997561468");
        _phone2 = new Phone("41997561468");
        _email = new Email("alysonullirsch8@gmail.com");
    }

    [TestMethod]
    public void deve_retornar_verdade_se_o_parametro_da_funcao_UpdateName_for_vazia()
    {
        var customer = new Customer(_name, _address, _phone, _phone2, _email);

        Assert.ThrowsException<EntityException<Customer>>(() =>
        {
            customer.UpdateName("");
        });
    }

    [TestMethod]
    public void deve_retornar_verdade_se_o_metodo_UpdateName_funcionar()
    {
        var customer = new Customer(_name, _address, _phone, _phone2, _email);
        customer.UpdateName("Francisco");

        Assert.AreEqual("Francisco", customer.Name.CustomerName);
    }

    [TestMethod]
    public void deve_retornar_verdade_se_o_parametro_da_funcao_UpdatePhone_for_vazia()
    {
        var customer = new Customer(_name, _address, _phone, _phone2, _email);

        Assert.ThrowsException<EntityException<Customer>>(() =>
        {
            customer.UpdatePhone("", "");
        });
    }

    [TestMethod]
    public void deve_retornar_verdade_se_o_metodo_UpdatePhone_funcionar()
    {
        var customer = new Customer(_name, _address, _phone, _phone2, _email);
        customer.UpdatePhone("4132923047", "");

        Assert.AreEqual("4132923047", customer.Phone.CustomerPhone);
    }
}