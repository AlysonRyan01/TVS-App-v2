using TVS_App.Application.DTOs;
using TVS_App.Domain.Entities;
using TVS_App.Domain.ValueObjects.Customer;

namespace TVS_App.Application.Mappers;

public static class CustomerMapper
{
    public static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name.CustomerName,
            Street = customer.Address.Street,
            Neighborhood = customer.Address.Neighborhood,
            City = customer.Address.City,
            Number = customer.Address.Number,
            ZipCode = customer.Address.ZipCode,
            State = customer.Address.State,
            Phone = customer.Phone.CustomerPhone,
            Phone2 = customer.Phone2.CustomerPhone,
            Email = customer.Email?.CustomerEmail,
            Cpf = customer.Cpf.Number
        };
    }

    public static Customer MapToEntity(CustomerDto customerDto)
    {
        var customer = new Customer(new Name(
                customerDto.Name),
            new Address(
                customerDto.Street, customerDto.Neighborhood, customerDto.City, customerDto.Number, customerDto.ZipCode,
                customerDto.State),
            new Phone(
                customerDto.Phone),
            new Phone(
                customerDto.Phone2),
            string.IsNullOrWhiteSpace(customerDto.Email) ? null : new Email(customerDto.Email),
            new Cpf(
                customerDto.Cpf));
        
        customer.Id = customerDto.Id;
        
        return customer;
    }
    
    public static List<CustomerDto> MapToDtoList(IEnumerable<Customer> customers)
    {
        return customers.Select(MapToDto).ToList();
    }

    public static List<Customer> MapToEntityList(IEnumerable<CustomerDto> customerDtos)
    {
        return customerDtos.Select(MapToEntity).ToList();
    }
}