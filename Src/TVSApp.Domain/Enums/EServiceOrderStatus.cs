using System.ComponentModel.DataAnnotations;

namespace TVS_App.Domain.Enums;

public enum EServiceOrderStatus
{
    [Display(Name = "Entrada")]
    Entered = 1,
    [Display(Name = "Avaliado")]
    Evaluated = 2,
    [Display(Name = "Consertado")]
    Repaired = 3,
    [Display(Name = "Entregue")]
    Delivered = 4,
    [Display(Name = "Aguardando peça")]
    OrderPart = 5
}