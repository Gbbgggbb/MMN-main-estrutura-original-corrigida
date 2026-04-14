using System.ComponentModel.DataAnnotations;

namespace MMN.Web.Models;

public enum ProspectTemperature
{
    [Display(Name = "Frio")]
    Cold = 1,

    [Display(Name = "Morno")]
    Warm = 2,

    [Display(Name = "Quente")]
    Hot = 3
}
