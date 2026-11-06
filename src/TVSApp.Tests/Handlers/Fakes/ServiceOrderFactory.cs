using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.ValueObjects.Customer;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVSApp.Tests.Handlers.Fakes;

public static class ServiceOrderFactory
{
    public static ServiceOrder Create(EEnterprise enterprise = EEnterprise.Particular)
    {
        var customer = new Customer(
            new Name("JOÃO DA SILVA"),
            new Address("RUA A", "CENTRO", "CURITIBA", "10", "80000000", "PR"),
            new Phone("41999999999"),
            new Phone("41988888888"),
            null,
            new Document("12345678909"))
        {
            Id = 7
        };

        var serviceOrder = new ServiceOrder(
            enterprise,
            customer.Id,
            new Product("LG", "42LB5600", "SN123", "NÃO LIGA", null, EProduct.Tv))
        {
            Id = 1
        };

        serviceOrder.UpdateCustomer(customer);
        return serviceOrder;
    }
}
