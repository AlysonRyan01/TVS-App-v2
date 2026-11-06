namespace TVS_App.Application.DTOs;

/// <summary>Endereço obtido pelo CEP (ViaCEP).</summary>
public record AddressDto(string ZipCode, string Street, string Neighborhood, string City, string State);
