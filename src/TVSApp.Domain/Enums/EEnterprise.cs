using System.ComponentModel.DataAnnotations;

namespace TVS_App.Domain.Enums;

public enum EEnterprise
{
    Particular = 1,
    Cocel = 3,
    Seguradora = 8,
    [Display(Name = "Philco/Britania")]
    PhilcoBritania = 16,
    Copel = 20
}