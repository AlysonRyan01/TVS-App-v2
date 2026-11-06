namespace TVS_App.Application.DTOs.ServiceOrder;

/// <summary>OS recém-criada: o número (para a tela) e o termo de entrada em PDF.</summary>
public record CreatedServiceOrderPdf(long Id, byte[] Pdf);
