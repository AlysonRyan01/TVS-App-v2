using System.ComponentModel.DataAnnotations;

namespace TVS_App.Domain.Enums;

public enum ERepairStatus
{
    [Display(Name = "Orçamento pendente")]
    Entered = 1,
    [Display(Name = "Aprovado")]
    Approved = 2,
    [Display(Name = "Reprovado")]
    Disapproved = 3,
    [Display(Name = "Aguardando resposta")]
    Waiting = 4,
}