using QuestPDF.Infrastructure;
using TVS_App.Domain.Entities;

namespace TVS_App.Infrastructure.Services.QuestPDFs;

public abstract class QuestPdf : IDocument
{
    private readonly ServiceOrder _serviceOrder;

    public QuestPdf(ServiceOrder serviceOrder)
    {
        _serviceOrder = serviceOrder;
    }

    public abstract void Compose(IDocumentContainer container);
}