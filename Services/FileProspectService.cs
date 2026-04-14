using System.Text.Json;
using MMN.Web.Models;

namespace MMN.Web.Services;

public class FileProspectService : IProspectService
{
    private readonly string _filePath;
    private readonly string _sharedChecklistFilePath;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly ICurrentUserService _currentUserService;

    public FileProspectService(IWebHostEnvironment environment, ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "prospects.json");
        _sharedChecklistFilePath = Path.Combine(dataDirectory, "shared-checklists.json");

        if (!File.Exists(_filePath))
        {
            SaveSeedData();
        }

        if (!File.Exists(_sharedChecklistFilePath))
        {
            SaveSharedChecklists([]);
        }
    }

    public IReadOnlyCollection<Prospect> GetAll()
    {
        lock (_lock)
        {
            var sharedChecklists = LoadSharedChecklists();
            return LoadProspects()
                .Select(prospect => ApplySharedChecklist(prospect, sharedChecklists))
                .Where(IsOwnedByCurrentUser)
                .OrderByDescending(p => p.Temperature)
                .ThenBy(p => p.NextContactDate)
                .ToList();
        }
    }

    public Prospect? GetById(Guid id)
    {
        lock (_lock)
        {
            var sharedChecklists = LoadSharedChecklists();
            return LoadProspects()
                .Select(prospect => ApplySharedChecklist(prospect, sharedChecklists))
                .FirstOrDefault(p => p.Id == id && IsOwnedByCurrentUser(p));
        }
    }

    public DashboardViewModel GetDashboard()
    {
        lock (_lock)
        {
            var sharedChecklists = LoadSharedChecklists();
            var prospects = LoadProspects()
                .Select(prospect => ApplySharedChecklist(prospect, sharedChecklists))
                .Where(IsOwnedByCurrentUser)
                .ToList();

            return new DashboardViewModel
            {
                TotalProspects = prospects.Count,
                HotProspects = prospects.Count(p => p.Temperature == ProspectTemperature.Hot),
                WarmProspects = prospects.Count(p => p.Temperature == ProspectTemperature.Warm),
                ColdProspects = prospects.Count(p => p.Temperature == ProspectTemperature.Cold),
                Customers = prospects.Count(p => p.Status == ProspectStatus.Customer || p.Status == ProspectStatus.PreferredCustomer),
                Consultants = prospects.Count(p => p.Status == ProspectStatus.WellnessConsultant),
                UpcomingContacts = prospects
                    .OrderBy(p => p.NextContactDate)
                    .Take(5)
                    .ToList(),
                RecentProspects = prospects
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(6)
                    .ToList(),
                PriorityContacts = prospects
                    .Where(p => p.Status != ProspectStatus.WellnessConsultant)
                    .OrderBy(p => p.NextContactDate)
                    .ThenByDescending(p => p.Temperature)
                    .Take(5)
                    .ToList()
            };
        }
    }

    public void Create(ProspectFormViewModel model)
    {
        lock (_lock)
        {
            var prospects = LoadProspects();
            var userId = _currentUserService.UserId;
            var email = _currentUserService.Email;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            prospects.Add(new Prospect
            {
                OwnerUserId = userId,
                OwnerEmail = email ?? string.Empty,
                FullName = model.FullName,
                Phone = model.Phone,
                Email = model.Email ?? string.Empty,
                City = model.City ?? string.Empty,
                Source = model.Source ?? string.Empty,
                Temperature = model.Temperature,
                Status = model.Status,
                NextContactDate = model.NextContactDate,
                Notes = model.Notes ?? string.Empty,
                ChecklistItems = NormalizeChecklist(model.ChecklistItems),
                CreatedAt = DateTime.Now
            });

            SaveProspects(prospects);
        }
    }

    public void Update(ProspectFormViewModel model)
    {
        if (model.Id is null)
        {
            return;
        }

        lock (_lock)
        {
            var prospects = LoadProspects();
            var prospect = prospects.FirstOrDefault(p => p.Id == model.Id.Value && IsOwnedByCurrentUser(p));
            if (prospect is null)
            {
                return;
            }

            prospect.FullName = model.FullName;
            prospect.Phone = model.Phone;
            prospect.Email = model.Email ?? string.Empty;
            prospect.City = model.City ?? string.Empty;
            prospect.Source = model.Source ?? string.Empty;
            prospect.Temperature = model.Temperature;
            prospect.Status = model.Status;
            prospect.NextContactDate = model.NextContactDate;
            prospect.Notes = model.Notes ?? string.Empty;
            prospect.ChecklistItems = NormalizeChecklist(model.ChecklistItems);

            SaveProspects(prospects);
        }
    }

    public void UpdateChecklist(Guid id, List<ChecklistItem> checklistItems)
    {
        lock (_lock)
        {
            var prospects = LoadProspects();
            var prospect = prospects.FirstOrDefault(p => p.Id == id && IsOwnedByCurrentUser(p));
            if (prospect is null)
            {
                return;
            }

            var sharedChecklists = LoadSharedChecklists();
            sharedChecklists[id] = NormalizeChecklist(checklistItems);
            SaveSharedChecklists(sharedChecklists);
        }
    }

    public void Delete(Guid id)
    {
        lock (_lock)
        {
            var prospects = LoadProspects();
            var prospect = prospects.FirstOrDefault(p => p.Id == id && IsOwnedByCurrentUser(p));
            if (prospect is null)
            {
                return;
            }

            prospects.Remove(prospect);
            SaveProspects(prospects);
        }
    }

    private List<Prospect> LoadProspects()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var prospects = JsonSerializer.Deserialize<List<Prospect>>(json, _jsonOptions) ?? [];

        foreach (var prospect in prospects)
        {
            prospect.ChecklistItems = NormalizeChecklist(prospect.ChecklistItems);
        }

        return prospects;
    }

    private void SaveProspects(List<Prospect> prospects)
    {
        var json = JsonSerializer.Serialize(prospects, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private Dictionary<Guid, List<ChecklistItem>> LoadSharedChecklists()
    {
        if (!File.Exists(_sharedChecklistFilePath))
        {
            return [];
        }

        var json = File.ReadAllText(_sharedChecklistFilePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var data = JsonSerializer.Deserialize<Dictionary<Guid, List<ChecklistItem>>>(json, _jsonOptions) ?? [];
        return data.ToDictionary(
            entry => entry.Key,
            entry => NormalizeChecklist(entry.Value));
    }

    private void SaveSharedChecklists(Dictionary<Guid, List<ChecklistItem>> sharedChecklists)
    {
        var json = JsonSerializer.Serialize(sharedChecklists, _jsonOptions);
        File.WriteAllText(_sharedChecklistFilePath, json);
    }

    private void SaveSeedData()
    {
        SaveProspects(
        [
            new()
            {
                OwnerUserId = "seed-user",
                OwnerEmail = "seed@mmn.local",
                FullName = "Carla Mendes",
                Phone = "(11) 98888-1122",
                Email = "carla@email.com",
                City = "Sao Paulo",
                Source = "Instagram",
                Temperature = ProspectTemperature.Hot,
                Status = ProspectStatus.PreferredCustomer,
                NextContactDate = DateTime.Today.AddDays(1),
                Notes = "Quer conhecer o plano e a possibilidade de renda extra.",
                ChecklistItems =
                [
                    new() { Title = "Conversar com o cliente", IsCompleted = true },
                    new() { Title = "Ligar para o cliente", IsCompleted = true },
                    new() { Title = "Apresentar produto" }
                ],
                CreatedAt = DateTime.Now.AddDays(-2)
            },
            new()
            {
                OwnerUserId = "seed-user",
                OwnerEmail = "seed@mmn.local",
                FullName = "Rafael Costa",
                Phone = "(21) 97777-3344",
                Email = "rafael@email.com",
                City = "Rio de Janeiro",
                Source = "Indicacao",
                Temperature = ProspectTemperature.Warm,
                Status = ProspectStatus.Prospect,
                NextContactDate = DateTime.Today,
                Notes = "Pediu retorno apos horario comercial.",
                ChecklistItems = ChecklistDefaults.Create(),
                CreatedAt = DateTime.Now.AddDays(-1)
            },
            new()
            {
                OwnerUserId = "seed-user",
                OwnerEmail = "seed@mmn.local",
                FullName = "Marina Alves",
                Phone = "(31) 96666-7788",
                Email = "marina@email.com",
                City = "Belo Horizonte",
                Source = "Landing page",
                Temperature = ProspectTemperature.Hot,
                Status = ProspectStatus.WellnessConsultant,
                NextContactDate = DateTime.Today.AddDays(3),
                Notes = "Ja viu apresentacao e quer avaliar com o esposo.",
                ChecklistItems =
                [
                    new() { Title = "Conversar com o cliente", IsCompleted = true },
                    new() { Title = "Ligar para o cliente", IsCompleted = true },
                    new() { Title = "Apresentar produto", IsCompleted = true }
                ],
                CreatedAt = DateTime.Now
            }
        ]);
    }

    private static List<ChecklistItem> NormalizeChecklist(List<ChecklistItem>? items)
    {
        var normalized = (items ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Title))
            .Select(item => new ChecklistItem
            {
                Title = item.Title.Trim(),
                IsCompleted = item.IsCompleted
            })
            .Take(ChecklistDefaults.MaxItems)
            .ToList();

        return normalized.Count == 0 ? ChecklistDefaults.Create() : normalized;
    }

    private static Prospect ApplySharedChecklist(Prospect prospect, IReadOnlyDictionary<Guid, List<ChecklistItem>> sharedChecklists)
    {
        if (!sharedChecklists.TryGetValue(prospect.Id, out var sharedChecklist))
        {
            return prospect;
        }

        prospect.ChecklistItems = NormalizeChecklist(sharedChecklist);
        return prospect;
    }

    private bool IsOwnedByCurrentUser(Prospect prospect)
    {
        var userId = _currentUserService.UserId;
        return !string.IsNullOrWhiteSpace(userId) && prospect.OwnerUserId == userId;
    }
}
