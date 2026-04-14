namespace MMN.Web.Models;

public class Prospect
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerUserId { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public ProspectStatus Status { get; set; } = ProspectStatus.Prospect;
    public string Source { get; set; } = string.Empty;
    public ProspectTemperature Temperature { get; set; } = ProspectTemperature.Warm;
    public DateTime NextContactDate { get; set; } = DateTime.Today.AddDays(2);
    public string Notes { get; set; } = string.Empty;
    public List<ChecklistItem> ChecklistItems { get; set; } = ChecklistDefaults.Create();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
