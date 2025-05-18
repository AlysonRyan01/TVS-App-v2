using System.ComponentModel.DataAnnotations;

namespace TVS_App.Domain.Enums;

public enum EProduct
{
    [Display(Name = "TV")]
    Tv = 1,
    [Display(Name = "CONTROLE REMOTO")]
    ControleRemoto = 5,
    [Display(Name = "SOM")]
    Som = 6,
    [Display(Name = "CAIXA ACÚSTICA")]
    CaixaAcustica = 10,
    [Display(Name = "MICROONDAS")]
    Microondas = 18
}