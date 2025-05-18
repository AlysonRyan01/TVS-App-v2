using System.ComponentModel.DataAnnotations;

namespace TVS_App.Domain.Enums;

public enum ERepairResult
{
    [Display(Name = "Tem conserto")]
    Repair = 1,
    [Display(Name = "Não tem conserto")]
    Unrepaired = 2,
    [Display(Name = "Não apresentou defeito")]
    NoDefectFound = 3
}