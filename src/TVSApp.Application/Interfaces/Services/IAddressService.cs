using TVS_App.Application.DTOs;
using TVS_App.Domain.Shared;
using TVS_App.Domain.ValueObjects.Customer;

namespace TVS_App.Application.Interfaces.Services;

public interface IAddressService
{
    Task<BaseResponse<AddressDto>> GetAddressByCep(string cep);
}