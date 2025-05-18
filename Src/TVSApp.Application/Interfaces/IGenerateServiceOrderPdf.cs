using TVS_App.Application.DTOs;
using TVS_App.Domain.Entities;

namespace TVS_App.Application.Interfaces;

public interface IGenerateServiceOrderPdf
{
    Task<BaseResponse<byte[]>> GenerateCheckInDocumentAsync(ServiceOrder serviceOrder);
    Task<BaseResponse<byte[]>> GenerateCheckOutDocumentAsync(ServiceOrder serviceOrder);
    Task<BaseResponse<byte[]>> RegeneratePdfAsync(ServiceOrder serviceOrder);
}