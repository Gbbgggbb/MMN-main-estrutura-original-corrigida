using MMN.Web.Models;

namespace MMN.Web.Services;

public interface IProspectService
{
    IReadOnlyCollection<Prospect> GetAll();
    Prospect? GetById(Guid id);
    DashboardViewModel GetDashboard();
    void Create(ProspectFormViewModel model);
    void Update(ProspectFormViewModel model);
    void UpdateChecklist(Guid id, List<ChecklistItem> checklistItems);
    void Delete(Guid id);
}
