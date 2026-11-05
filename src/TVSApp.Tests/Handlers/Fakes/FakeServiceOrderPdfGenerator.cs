using TVS_App.Application.Interfaces.Services;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVSApp.Tests.Handlers.Fakes;

public class FakeServiceOrderPdfGenerator : IGenerateServiceOrderPdf
{
    public BaseResponse<byte[]> CheckOutResponse { get; set; } =
        new(new byte[] { 0x25, 0x50, 0x44, 0x46 }, 200, "PDF gerado");

    public int CheckOutCalls { get; private set; }

    public Task<BaseResponse<byte[]>> GenerateCheckOutDocumentAsync(ServiceOrder serviceOrder)
    {
        CheckOutCalls++;
        return Task.FromResult(CheckOutResponse);
    }

    public Task<BaseResponse<byte[]>> GenerateCheckInDocumentAsync(ServiceOrder serviceOrder) => throw new NotImplementedException();
    public Task<BaseResponse<byte[]>> GenerateSaleDocumentAsync(ServiceOrder serviceOrder) => throw new NotImplementedException();
    public Task<BaseResponse<byte[]>> RegeneratePdfAsync(ServiceOrder serviceOrder) => throw new NotImplementedException();
}
