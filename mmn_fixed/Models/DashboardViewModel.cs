namespace MMN.Web.Models;

public class DashboardViewModel
{
    public int TotalProspects { get; set; }
    public int HotProspects { get; set; }
    public int WarmProspects { get; set; }
    public int ColdProspects { get; set; }
    public int Customers { get; set; }
    public int Consultants { get; set; }
    public IReadOnlyCollection<Prospect> UpcomingContacts { get; set; } = Array.Empty<Prospect>();
    public IReadOnlyCollection<Prospect> RecentProspects { get; set; } = Array.Empty<Prospect>();
    public IReadOnlyCollection<Prospect> PriorityContacts { get; set; } = Array.Empty<Prospect>();
}
