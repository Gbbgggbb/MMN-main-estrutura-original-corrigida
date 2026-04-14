using System.ComponentModel.DataAnnotations;

namespace MMN.Web.Models;

public enum ProspectStatus
{
    [Display(Name = "Prospecto")]
    Prospect = 1,

    [Display(Name = "Cliente")]
    Customer = 2,

    [Display(Name = "Cliente preferencial")]
    PreferredCustomer = 3,

    [Display(Name = "Consultor de bem-estar")]
    WellnessConsultant = 4
}
