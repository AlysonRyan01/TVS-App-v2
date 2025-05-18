using TVS_App.Application.DTOs;
using TVS_App.Application.Repositories;
using TVS_App.Domain.Entities;
using TVS_App.Domain.ValueObjects.Customer;

namespace TVS_App.Tests.Mocks;

public class FakeCustomerRepository : ICustomerRepository
{
    private readonly List<Customer> _customers = new();
    private long _idCounter = 1;

    public FakeCustomerRepository()
    {
        CreateAsync(new Customer(
            new Name("Alyson"),
            new Address("Rua Centenario", "Centro", "Campo Largo", "123", "83601000", "Parana"),
            new Phone("41997561468"),
            new Phone("41997561468"),
            new Email("alysonullirsch8@gmail.com")));

        CreateAsync(new Customer(new Name("Bruna Lima"),
            new Address("Av. das Flores", "Jardim América", "Curitiba", "456", "80050000", "Paraná"),
            new Phone("41999998888"), new Phone("41997561468"), new Email("bruna.lima@email.com")));

        CreateAsync(new Customer(new Name("Carlos Eduardo"),
            new Address("Rua Rio Branco", "Centro", "Ponta Grossa", "789", "84010000", "Paraná"),
            new Phone("42988887777"), new Phone("41997561468"), new Email("carlos.edu@email.com")));

        CreateAsync(new Customer(new Name("Daniela Souza"),
            new Address("Rua XV de Novembro", "Batel", "Curitiba", "321", "80420000", "Paraná"),
            new Phone("41977776666"), new Phone("41997561468"), new Email("daniela.souza@email.com")));

        CreateAsync(new Customer(new Name("Eduardo Silva"),
            new Address("Av. Paraná", "Boa Vista", "Curitiba", "654", "82520000", "Paraná"),
            new Phone("41966665555"), new Phone("41997561468"), new Email("eduardo.silva@email.com")));

        CreateAsync(new Customer(new Name("Fernanda Rocha"),
            new Address("Rua das Palmeiras", "Água Verde", "Curitiba", "147", "80620000", "Paraná"),
            new Phone("41955554444"), new Phone("41997561468"), new Email("fernanda.rocha@email.com")));

        CreateAsync(new Customer(new Name("Gustavo Martins"),
            new Address("Av. Sete de Setembro", "Rebouças", "Curitiba", "258", "80230000", "Paraná"),
            new Phone("41944443333"), new Phone("41997561468"), new Email("gustavo.martins@email.com")));

        CreateAsync(new Customer(new Name("Helena Castro"),
            new Address("Rua Marechal Deodoro", "Centro", "São José dos Pinhais", "369", "83005000", "Paraná"),
            new Phone("41933332222"), new Phone("41997561468"), new Email("helena.castro@email.com")));

        CreateAsync(new Customer(new Name("Igor Fernandes"),
            new Address("Rua Getúlio Vargas", "Centro", "Araucária", "741", "83702000", "Paraná"),
            new Phone("41922221111"), new Phone("41997561468"), new Email("igor.fernandes@email.com")));

        CreateAsync(new Customer(new Name("Juliana Oliveira"),
            new Address("Av. Brasília", "Costeira", "Araucária", "852", "83703000", "Paraná"),
            new Phone("41911110000"), new Phone("41997561468"), new Email("juliana.oliveira@email.com")));

        CreateAsync(new Customer(new Name("Kleber Ramos"),
            new Address("Rua das Rosas", "Cristo Rei", "Curitiba", "963", "80050000", "Paraná"),
            new Phone("41900009999"), new Phone("41997561468"), new Email("kleber.ramos@email.com")));
    }

    public Task<BaseResponse<Customer?>> CreateAsync(Customer customer)
    {
        customer.Id = _idCounter++;
        _customers.Add(customer);
        return Task.FromResult(new BaseResponse<Customer?>(customer, 200, "Cliente obtido com sucesso!"));
    }

    public Task<BaseResponse<Customer?>> UpdateAsync(Customer customer)
    {
        var index = _customers.FindIndex(c => c.Id == customer.Id);
        if (index == -1)
            return Task.FromResult(new BaseResponse<Customer?>(null, 404, "Cliente não encontrado"));

        _customers[index] = customer;
        return Task.FromResult(new BaseResponse<Customer?>(customer, 200, "Cliente obtido com sucesso!"));
    }

    public Task<BaseResponse<Customer?>> GetByIdAsync(long id)
    {
        var customer = _customers.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(new BaseResponse<Customer?>(customer, 200, "Cliente obtido com sucesso!"));
    }

    public Task<BaseResponse<List<Customer>>> GetCustomerByName(string name)
    {
        throw new NotImplementedException();
    }

    public Task<BaseResponse<PaginatedResult<Customer?>>> GetAllAsync(int pageNumber, int pageSize)
    {
        int totalCount = _customers.Count;

        var paged = _customers
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PaginatedResult<Customer?>(paged, totalCount, pageNumber, pageSize);

        return Task.FromResult(new BaseResponse<PaginatedResult<Customer?>>(result, 200, "Todos os clientes foram obtidos com sucesso!"));
    }
}
