namespace TVS_App.Application.DTOs;

public record CustomerDto
{
    public long Id { get; init; }
    
    public string Name { get; init; } = string.Empty;
    
    public string Street { get; init; } = string.Empty;
    public string Neighborhood { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    
    public string Phone { get; init; } = string.Empty;
    public string Phone2 { get; init; } = string.Empty;
    
    public string? Email { get; init; }
    
    public string Cpf { get; init; } = string.Empty;
}