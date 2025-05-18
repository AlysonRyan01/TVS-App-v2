using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TVS_App.Domain.Entities;
using TVS_App.Infrastructure.Extensions;

namespace TVS_App.Infrastructure.Services.QuestPDFs;

public class GenerateCheckInDocument : QuestPdf
{
    private readonly ServiceOrder _serviceOrder;

    public GenerateCheckInDocument(ServiceOrder serviceOrder) : base(serviceOrder)
    {
        _serviceOrder = serviceOrder;
    }

    public override void Compose(IDocumentContainer document)
    {
        try
        {
            var qrCodeBytes = GenerateQrCodeService.GenerateImage($"https://tvseletronica.com/consultar-os?id={_serviceOrder.Id}&code={_serviceOrder.SecurityCode}");

            document.Page(page =>
            {
                page.Margin(12);

                page.Content().Column(column =>
                {   
                    column.Item().Element(container =>
                    {
                        container.Row(row =>
                        {
                            row.Spacing(0);
                            row.ConstantItem(500).Text("TVS Eletrônica")
                                .FontSize(40)
                                .ExtraBold()
                                .FontColor(Colors.Red.Darken2)
                                .Underline();

                            row.RelativeItem(); 

                            row.ConstantItem(50).Column(col =>
                            {
                                col.Item().Image(qrCodeBytes);

                                col.Item().Text($"{_serviceOrder.SecurityCode}")
                                    .FontSize(10)
                                    .ExtraBold()
                                    .FontColor(Colors.Red.Darken2)
                                    .AlignCenter();
                            });
                        });
                    });

                    column.Item().Element(container =>
                    {
                        container.Row(row =>
                        {
                            row.Spacing(0);
                            row.ConstantItem(140).Text("WWW.TVSELETRONICA.COM")
                                .FontSize(9)
                                .ExtraBold();
                            row.ConstantItem(120).Text("FIXO: (41) 3292-3047")
                                .FontSize(9)
                                .ExtraBold();

                            row.ConstantItem(200).Text("WHATSAPP: (41) 99274-4920")
                                .FontSize(9)
                                .ExtraBold();
                        });
                    });

                    column.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Black);
                    
                    column.Item().Element(container =>
                    {
                        container.Row(row =>
                        {
                            row.ConstantItem(350).Column(col =>
                            {
                                col.Spacing(4);
                                col.Item().Text($"ORDEM DE SERVIÇO N:  {_serviceOrder.Id.ToString("00'.'000")}")
                                    .FontSize(12)
                                    .ExtraBold()
                                    .FontColor(Colors.Red.Darken2);

                                col.Item().Text($"EMPRESA:  {_serviceOrder.Enterprise.GetDisplayName()}")
                                    .FontSize(10)
                                    .ExtraBold()
                                    .FontColor(Colors.Red.Darken2);

                                col.Item().Text($"Cliente:  {_serviceOrder.Customer.Name.CustomerName.ToUpper()}")
                                    .FontSize(10)
                                    .SemiBold();

                                col.Item().Text($"Endereço:  {_serviceOrder.Customer.Address.ToString().ToUpper()}")
                                    .FontSize(10)
                                    .SemiBold();

                                col.Item().Text($"Cidade:  {_serviceOrder.Customer.Address.City.ToUpper()}")
                                    .FontSize(10)
                                    .SemiBold();

                                col.Item().Text($"Fone:  {_serviceOrder.Customer.Phone.CustomerPhone.ToUpper()}")
                                    .FontSize(10)
                                    .SemiBold();

                                col.Item().Text($"Fone:  {_serviceOrder.Customer.Phone2.CustomerPhone.ToUpper()}")
                                    .FontSize(10)
                                    .SemiBold();

                            });

                            row.ConstantItem(200).Column(col =>
                            {
                                col.Spacing(4);
                                col.Item().Text($"DATA:  {_serviceOrder.EntryDate.ToString("dd/MM/yyyy")}")
                                    .FontSize(10)
                                    .ExtraBold()
                                    .FontColor(Colors.Red.Darken2);

                                col.Item().Text($"Aparelho:  {_serviceOrder.Product.Type.GetDisplayName()}")
                                    .FontSize(10)
                                    .SemiBold();

                                col.Item().Text($"Marca:  {_serviceOrder.Product.Brand.ToUpper()}")
                                    .FontSize(10)
                                    .SemiBold();

                                col.Item().Text($"Modelo:  {_serviceOrder.Product.Model.ToUpper()}")
                                    .FontSize(10)
                                    .SemiBold();

                                col.Item().Text($"Série:  {_serviceOrder.Product.SerialNumber.ToUpper()}")
                                    .FontSize(10)
                                    .SemiBold();

                                col.Item().Text($"Defeito:  {_serviceOrder.Product.Defect?.ToUpper() ?? ""}")
                                    .FontSize(10)
                                    .SemiBold();

                                col.Item().Text($"Acessórios:  {_serviceOrder.Product.Accessories?.ToUpper() ?? ""}")
                                    .FontSize(10)
                                    .SemiBold();
                            });

                            column.Item().PaddingTop(30).LineHorizontal(1).LineColor(Colors.Black);

                            column.Item().Element(container =>
                            {
                                container.Row(row =>
                                {
                                    row.Spacing(0);
                                    row.ConstantItem(400).Text($"{_serviceOrder.Id.ToString("00'.'000")}")
                                        .FontSize(120)
                                        .ExtraBold()
                                        .FontColor(Colors.Red.Darken3).Justify();
                                });
                            });

                            column.Item().PaddingVertical(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        });
                    });
                });
            });
        }
        catch (Exception ex)
        {
            throw new Exception($"Ocorreu um erro ao gerar o pdf de entrada: {ex.Message}");
        }
    }
}