using TVS_App.Domain.Exceptions;
using TVS_App.Domain.ValueObjects.Customer;

namespace TVS_App.Domain.Entities;

public class Customer : Entity
{
    private readonly List<ServiceOrder> _serviceOrders = new();

    protected Customer() { }
    
    public Customer(Name name, Address address, Phone phone, Phone phone2, Email? email, Cpf cpf)
    {
        Name = name;
        Address = address;
        Phone = phone;
        Phone2 = phone2;
        Email = email;
        Cpf = cpf;
    }
    
    public Name Name { get; private set; } = null!;
    public Address Address { get; private set; } = null!;
    public Phone Phone { get; private set; } = null!;
    public Phone Phone2 { get; private set; } = null!;
    public Email? Email { get; private set; }
    public Cpf Cpf { get; private set; } = null!;
    
    public IReadOnlyCollection<ServiceOrder> ServiceOrders => _serviceOrders.AsReadOnly();

    public void UpdateName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new EntityException<Customer>("Não podemos atualizar o nome pois está vazio");

        Name = new Name(name);
    }

    public void UpdateAdress(string street, string neighborhood, string city, string number, string zipCode, string state)
    {
        Address = new Address(street, neighborhood, city, number, zipCode, state);
    }

    public void UpdatePhone(string number1, string number2)
    {
        Phone = new Phone(number1);
        Phone2 = new Phone(number2);
    }

    public void UpdateEmail(string email)
    {
        Email = new Email(email);
    }

    public void UpdateCpf(string cpf)
    {
        Cpf = new Cpf(cpf);
    }
    
    public void AddServiceOrder(ServiceOrder serviceOrder)
    {
        if (serviceOrder == null)
            throw new ArgumentNullException(nameof(serviceOrder));

        _serviceOrders.Add(serviceOrder);
    }
}