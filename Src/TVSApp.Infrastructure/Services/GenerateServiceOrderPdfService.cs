using QuestPDF.Fluent;
using TVS_App.Application.DTOs;
using TVS_App.Application.Interfaces;
using TVS_App.Domain.Entities;
using TVS_App.Infrastructure.Services.QuestPDFs;

namespace TVS_App.Infrastructure.Services;

public class GenerateServiceOrderPdfService : IGenerateServiceOrderPdf
{
    public Task<BaseResponse<byte[]>> GenerateCheckInDocumentAsync(ServiceOrder serviceOrder)
    {
        try
        {
            var document = new GenerateCheckInDocument(serviceOrder);

            byte[] pdfBytes = document.GeneratePdf();

            return Task.FromResult(new BaseResponse<byte[]>(pdfBytes, 200, "Pdf gerado com sucesso!"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new BaseResponse<byte[]>(null, 500, $"Ocorreu um erro ao gerar o PDF de entrada na classe GenerateServiceOrderPdfService: {ex.Message}"));
        }
    }

    public Task<BaseResponse<byte[]>> GenerateCheckOutDocumentAsync(ServiceOrder serviceOrder)
    {
        try
        {
            var document = new GenerateCheckOutDocument(serviceOrder);

            byte[] pdfBytes = document.GeneratePdf();

            return Task.FromResult(new BaseResponse<byte[]>(pdfBytes, 200, "Pdf gerado com sucesso!"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new BaseResponse<byte[]>(null, 500, $"Ocorreu um erro ao gerar o PDF de saída na classe GenerateServiceOrderPdfService: {ex.Message}"));
        }
    }

    public Task<BaseResponse<byte[]>> RegeneratePdfAsync(ServiceOrder serviceOrder)
    {
        try
        {
            var document = new RegenerateDocument(serviceOrder);

            byte[] pdfBytes = document.GeneratePdf();

            return Task.FromResult(new BaseResponse<byte[]>(pdfBytes, 200, "Pdf gerado com sucesso!"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new BaseResponse<byte[]>(null, 500, $"Ocorreu um erro ao gerar o PDF na classe GenerateServiceOrderPdfService: {ex.Message}"));
        }
    }
}
        
    