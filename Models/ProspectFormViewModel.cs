using System.ComponentModel.DataAnnotations;

namespace MMN.Web.Models;

public class ProspectFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Informe o nome do prospecto.")]
    [Display(Name = "Nome completo")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o telefone.")]
    [Display(Name = "Telefone")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Informe um e-mail valido.")]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    [Display(Name = "Cidade")]
    public string? City { get; set; }

    [Display(Name = "Origem do lead")]
    public string? Source { get; set; }

    [Display(Name = "Temperatura")]
    public ProspectTemperature Temperature { get; set; } = ProspectTemperature.Warm;

    [Display(Name = "Status")]
    public ProspectStatus Status { get; set; } = ProspectStatus.Prospect;

    [Display(Name = "Proximo contato")]
    [DataType(DataType.Date)]
    public DateTime NextContactDate { get; set; } = DateTime.Today.AddDays(2);

    [Display(Name = "Observacoes")]
    public string? Notes { get; set; }

    public List<ChecklistItem> ChecklistItems { get; set; } = ChecklistDefaults.Create();
}
