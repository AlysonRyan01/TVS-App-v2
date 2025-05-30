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
    Microondas = 18,
    [Display(Name = "FONTE")]
    Fonte = 19,
    [Display(Name = "BASES")]
    Bases = 20,
    [Display(Name = "PLACA FONTE")]
    PlacaFonte = 21,
    [Display(Name = "PLACA PRINCIPAL")]
    PlacaPrincipal = 22
}