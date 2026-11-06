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

    public BaseResponse<byte[]> CheckInResponse { get; set; } =
        new(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }, 200, "PDF de entrada gerado");

    public int CheckInCalls { get; private set; }

    public Task<BaseResponse<byte[]>> GenerateCheckInDocumentAsync(ServiceOrder serviceOrder)
    {
        CheckInCalls++;
        return Task.FromResult(CheckInResponse);
    }
    public BaseResponse<byte[]> SaleResponse { get; set; } =
        new(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x56 }, 200, "Comprovante de venda gerado");

    public int SaleCalls { get; private set; }

    public Task<BaseResponse<byte[]>> GenerateSaleDocumentAsync(ServiceOrder serviceOrder)
    {
        SaleCalls++;
        return Task.FromResult(SaleResponse);
    }

    public BaseResponse<byte[]> RegenerateResponse { get; set; } =
        new(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x52 }, 200, "PDF gerado");

    public int RegenerateCalls { get; private set; }

    public Task<BaseResponse<byte[]>> RegeneratePdfAsync(ServiceOrder serviceOrder)
    {
        RegenerateCalls++;
        return Task.FromResult(RegenerateResponse);
    }

    public BaseResponse<byte[]> ReceiptResponse { get; set; } =
        new(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x43 }, 200, "Comprovante gerado");

    public int ReceiptCalls { get; private set; }
    public long? LastReceiptPaymentId { get; private set; }

    public Task<BaseResponse<byte[]>> GeneratePaymentReceiptAsync(ServiceOrder serviceOrder, Payment payment)
    {
        ReceiptCalls++;
        LastReceiptPaymentId = payment.Id;
        return Task.FromResult(ReceiptResponse);
    }
}
