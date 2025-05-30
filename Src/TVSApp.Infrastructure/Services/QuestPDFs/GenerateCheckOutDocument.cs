using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TVS_App.Domain.Entities;
using TVS_App.Infrastructure.Extensions;

namespace TVS_App.Infrastructure.Services.QuestPDFs;

public class GenerateCheckOutDocument : QuestPdf
{
    private readonly ServiceOrder _serviceOrder;

    public GenerateCheckOutDocument(ServiceOrder serviceOrder) : base(serviceOrder)
    {
        _serviceOrder = serviceOrder;
    }

    public override void Compose(IDocumentContainer document)
    {
        try
        {
            document.Page(page =>
            {
                page.Margin(8);

                page.Content().Column(column =>
                {
                    column.Item().Element(container =>
                    {
                        container.Row(row =>
                        {
                            row.Spacing(0);
                            row.ConstantItem(400).Text("TVS ELETRÔNICA")
                                .FontSize(40)
                                .Underline()
                                .ExtraBold()
                                .FontFamily("Arial")
                                .FontColor(Colors.Red.Darken2);
                        });
                    });
                    
                    column.Item().PaddingVertical(3);

                    column.Item().Element(container =>
                    {
                        container.Row(row =>
                        {
                            row.Spacing(0);
                            row.ConstantItem(140).Text("WWW.TVSELETRONICA.COM")
                                .FontSize(8)
                                .ExtraBold()
                                .Underline();
                            row.ConstantItem(120).Text("FONE: (41) 3292-3047")
                                .FontSize(8)
                                .ExtraBold();
                            row.ConstantItem(200).Text("WHATSAPP: (41) 99274-4920")
                                .FontSize(8)
                                .ExtraBold();
                        });
                    });

                    column.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Red.Lighten2);

                    column.Item().Element(container =>
                    {
                        container.Row(row =>
                        {
                            row.ConstantItem(350).Column(col =>
                            {
                                col.Spacing(4);
                                col.Item().Text($"ORDEM DE SERVIÇO N:  {_serviceOrder.Id:00'.'000}")
                                    .FontSize(12)
                                    .ExtraBold()
                                    .FontFamily("Arial")
                                    .FontColor(Colors.Red.Darken2);

                                col.Item().Text($"EMPRESA:  {_serviceOrder.Enterprise.GetDisplayName()}")
                                    .FontSize(12)
                                    .ExtraBold()
                                    .FontFamily("Arial")
                                    .FontColor(Colors.Red.Darken2);

                                col.Item().PaddingTop(10).Text("DADOS DO CLIENTE")
                                    .FontSize(10)
                                    .ExtraBold()
                                    .FontColor(Colors.Red.Darken2);

                                col.Item().Text($"Cliente:  {_serviceOrder.Customer?.Name?.CustomerName.ToUpper() ?? "-"}")
                                    .FontSize(10);

                                col.Item().Text($"Endereço:  {_serviceOrder.Customer?.Address?.Street?.ToUpper() ?? "-"}, " +
                                                $"{_serviceOrder.Customer?.Address?.Number ?? ""}, " +
                                                $"{_serviceOrder.Customer?.Address?.Neighborhood?.ToUpper() ?? "-"}")
                                    .FontSize(10);

                                col.Item().Text($"Cidade:  {_serviceOrder.Customer?.Address?.City?.ToUpper() ?? "-"}")
                                    .FontSize(10);

                                col.Item().Text($"Fone:  {_serviceOrder.Customer?.Phone.CustomerPhone?.ToUpper() ?? "-"}")
                                    .FontSize(10);

                                col.Item().Text($"Fone:  {_serviceOrder.Customer?.Phone2?.CustomerPhone.ToUpper() ?? "-"}")
                                    .FontSize(10);
                            });

                            row.ConstantItem(200).Column(col =>
                            {
                                col.Spacing(4);
                                col.Item().Text($"DATA:  {_serviceOrder.EntryDate:dd/MM/yyyy}")
                                    .FontSize(12)
                                    .ExtraBold()
                                    .FontFamily("Arial")
                                    .FontColor(Colors.Red.Darken2);

                                col.Item().PaddingTop(10).Text("DADOS DO PRODUTO")
                                    .FontSize(10)
                                    .ExtraBold()
                                    .FontColor(Colors.Red.Darken2);

                                col.Item().Text($"Aparelho:  {_serviceOrder.Product?.Type.GetDisplayName() ?? "-"}")
                                    .FontSize(10);
                                col.Item().Text($"Marca:  {_serviceOrder.Product?.Brand?.ToUpper() ?? "-"}")
                                    .FontSize(10);
                                col.Item().Text($"Modelo:  {_serviceOrder.Product?.Model?.ToUpper() ?? "-"}")
                                    .FontSize(10);
                                col.Item().Text($"Série:  {_serviceOrder.Product?.SerialNumber?.ToUpper() ?? "-"}")
                                    .FontSize(10);
                                col.Item().Text($"Defeito:  {_serviceOrder.Product?.Defect?.ToUpper() ?? "-"}")
                                    .FontSize(10);
                                col.Item().Text($"Acessórios:  {_serviceOrder.Product?.Accessories?.ToUpper() ?? "-"}")
                                    .FontSize(10);
                            });
                        });
                    });

                    column.Item().PaddingVertical(20).LineHorizontal(1).LineColor(Colors.Red.Lighten2);

                    column.Item().PaddingVertical(10).Element(container =>
                    {
                        container.Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                        {
                            // Coluna de Informações Técnicas
                            row.RelativeItem().Column(col =>
                            {
                                col.Spacing(8);
                                
                                // Solução
                                col.Item().Row(r => 
                                {
                                    r.ConstantItem(80).Text("SOLUÇÃO:").FontSize(11).SemiBold().FontColor(Colors.Grey.Darken2);
                                    r.RelativeItem().Text(_serviceOrder.Solution?.ServiceOrderSolution?.ToUpper() ?? "-")
                                        .FontSize(11).Bold().FontColor(Colors.Black);
                                });
                                
                                // Garantia
                                col.Item().Row(r => 
                                {
                                    r.ConstantItem(80).Text("GARANTIA:").FontSize(11).SemiBold().FontColor(Colors.Grey.Darken2);
                                    r.RelativeItem().Text(_serviceOrder.Guarantee?.ServiceOrderGuarantee?.ToUpper() ?? "-")
                                        .FontSize(11).Bold().FontColor(Colors.Black);
                                });
                                
                                // Técnico
                                col.Item().Row(r => 
                                {
                                    r.ConstantItem(80).Text("TÉCNICO:").FontSize(11).SemiBold().FontColor(Colors.Grey.Darken2);
                                    r.RelativeItem().Text("PEDRO ULLIRSCH")
                                        .FontSize(11).Bold().FontColor(Colors.Black);
                                });
                                
                                // Data de Entrega
                                col.Item().Row(r => 
                                {
                                    r.ConstantItem(80).Text("ENTREGA:").FontSize(11).SemiBold().FontColor(Colors.Grey.Darken2);
                                    r.RelativeItem().Text(_serviceOrder.DeliveryDate?.ToString("dd/MM/yyyy") ?? "-")
                                        .FontSize(11).Bold().FontColor(Colors.Black);
                                });
                            });

                            // Coluna do Valor (destaque)
                            row.ConstantItem(200).Column(col => 
                            {
                                col.Item().AlignCenter().Background(Colors.Red.Lighten5).Padding(10).Column(valueCol => 
                                {
                                    valueCol.Item().AlignCenter().Text("VALOR TOTAL")
                                        .FontSize(12)
                                        .SemiBold()
                                        .FontColor(Colors.Grey.Darken2);
                                    
                                    valueCol.Item().AlignCenter().Text(_serviceOrder.TotalAmount.ToString("C", new CultureInfo("pt-BR")))
                                        .FontSize(18)
                                        .ExtraBold()
                                        .FontColor(Colors.Red.Darken3);
                                    
                                });
                            });
                        });
                    });
                    column.Item().PaddingVertical(20).LineHorizontal(1).LineColor(Colors.Red.Lighten2);
                });
            });
        }
        catch (Exception ex)
        {

            throw new Exception($"Ocorreu um erro ao gerar o pdf de saída: {ex.Message}");
        }
    }
}