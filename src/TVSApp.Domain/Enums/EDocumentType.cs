using System.ComponentModel.DataAnnotations;

namespace TVS_App.Domain.Enums;

public enum EDocumentType
{
    [Display(Name = "CPF")]
    Cpf = 1,
    [Display(Name = "CNPJ")]
    Cnpj = 2
}
