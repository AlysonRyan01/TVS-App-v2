using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TVS_App.Application.Services;
using TVS_App.Domain.Entities;
using TVS_App.Infrastructure.Extensions;

namespace TVS_App.Infrastructure.Services.QuestPDFs;

public class GeneratePaymentReceiptDocument : IDocument
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly ServiceOrder _serviceOrder;
    private readonly Payment _payment;
    private readonly PaymentReceiptSummary _summary;

    public GeneratePaymentReceiptDocument(ServiceOrder serviceOrder, Payment payment)
    {
        _serviceOrder = serviceOrder;
        _payment = payment;
        _summary = PaymentReceiptSummary.For(serviceOrder, payment);
    }

    public void Compose(IDocumentContainer document)
    {
        document.Page(page =>
        {
            page.Margin(24);

            page.Content().Column(column =>
            {
                column.Spacing(6);

                column.Item().Text("TVS ELETRÔNICA")
                    .FontSize(36).Underline().ExtraBold().FontFamily("Arial").FontColor(Colors.Red.Darken2);

                column.Item().Row(row =>
                {
                    row.ConstantItem(140).Text("WWW.TVSELETRONICA.COM").FontSize(8).Underline().ExtraBold();
                    row.ConstantItem(120).Text("FONE: (41) 3292-3047").FontSize(8).ExtraBold();
                    row.ConstantItem(200).Text("WHATSAPP: (41) 99274-4920").FontSize(8).ExtraBold();
                });

                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Red.Lighten2);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"COMPROVANTE DE PAGAMENTO Nº {_payment.Id}")
                        .FontSize(14).ExtraBold().FontFamily("Arial").FontColor(Colors.Red.Darken2);
                    row.ConstantItem(170).AlignRight().Text($"ORDEM DE SERVIÇO: {_serviceOrder.Id}")
                        .FontSize(12).ExtraBold().FontColor(Colors.Red.Darken2);
                });

                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Spacing(3);
                        col.Item().Text("CLIENTE").FontSize(10).ExtraBold().FontColor(Colors.Red.Darken2);
                        col.Item().Text($"Nome:  {_serviceOrder.Customer?.Name?.CustomerName?.ToUpper() ?? "-"}").FontSize(10);
                        col.Item().Text($"Fone:  {_serviceOrder.Customer?.Phone?.CustomerPhone ?? "-"}").FontSize(10);
                        col.Item().Text($"CPF/CNPJ:  {_serviceOrder.Customer?.Document?.ToString() ?? "-"}").FontSize(10);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Spacing(3);
                        col.Item().Text("APARELHO").FontSize(10).ExtraBold().FontColor(Colors.Red.Darken2);
                        col.Item().Text($"Aparelho:  {_serviceOrder.Product.Type.GetDisplayName()}").FontSize(10);
                        col.Item().Text($"Marca / Modelo:  {_serviceOrder.Product.Brand?.ToUpper()} {_serviceOrder.Product.Model?.ToUpper()}").FontSize(10);
                        col.Item().Text($"Série:  {_serviceOrder.Product.SerialNumber?.ToUpper() ?? "-"}").FontSize(10);
                    });
                });

                column.Item().PaddingVertical(14).Element(container =>
                {
                    container.Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(12).Column(col =>
                    {
                        col.Spacing(4);
                        col.Item().Text("VALOR RECEBIDO").FontSize(11).SemiBold().FontColor(Colors.Grey.Darken2);
                        col.Item().Text(_payment.Amount.ToString("C", PtBr))
                            .FontSize(22).ExtraBold().FontColor(Colors.Red.Darken3);
                        col.Item().Text($"({ValorPorExtenso.Format(_payment.Amount)})").FontSize(10).Italic();
                        col.Item().PaddingTop(4).Text($"Data do pagamento:  {_payment.PaidAt:dd/MM/yyyy}").FontSize(11).SemiBold();
                    });
                });

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(150);
                    });

                    void Line(string label, string value, bool strong = false)
                    {
                        table.Cell().PaddingVertical(3).Text(label).FontSize(11);
                        var cell = table.Cell().PaddingVertical(3).AlignRight().Text(value).FontSize(11);
                        if (strong)
                            cell.ExtraBold();
                    }

                    Line("Valor da ordem de serviço", _summary.ServiceOrderTotal.ToString("C", PtBr));
                    Line("Total pago até este pagamento", _summary.PaidUntilThisPayment.ToString("C", PtBr));
                    if (_summary.Balance > 0)
                        Line("Saldo restante", _summary.Balance.ToString("C", PtBr), strong: true);
                    else if (_summary.Balance < 0)
                        Line("Crédito do cliente", (-_summary.Balance).ToString("C", PtBr), strong: true);
                    else
                        Line("Saldo restante", "Quitado", strong: true);
                });

                column.Item().PaddingTop(40).AlignCenter().Width(260).LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                column.Item().AlignCenter().Text("TVS ELETRÔNICA").FontSize(9);

                column.Item().PaddingTop(20).AlignRight()
                    .Text($"Emitido em {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        });
    }
}
