using Google.Cloud.Firestore;
using Google;
using MMN.Web.Models;

namespace MMN.Web.Services;

public class FirestoreProspectService : IProspectService
{
    private readonly FirestoreDb _firestoreDb;
    private readonly ICurrentUserService _currentUserService;
    private const string SharedChecklistCollectionName = "sharedProspectChecklists";

    public FirestoreProspectService(FirestoreDb firestoreDb, ICurrentUserService currentUserService)
    {
        _firestoreDb = firestoreDb;
        _currentUserService = currentUserService;
    }

    public IReadOnlyCollection<Prospect> GetAll()
    {
        try
        {
            var prospects = GetAllInternalAsync().GetAwaiter().GetResult();
            return prospects
                .OrderByDescending(p => p.Temperature)
                .ThenBy(p => p.NextContactDate)
                .ToList();
        }
        catch (Exception ex)
        {
            throw ToUserFacingException(ex);
        }
    }

    public Prospect? GetById(Guid id)
    {
        try
        {
            return GetByIdInternalAsync(id).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw ToUserFacingException(ex);
        }
    }

    public DashboardViewModel GetDashboard()
    {
        try
        {
            var prospects = GetAllInternalAsync().GetAwaiter().GetResult();

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
        catch (Exception ex)
        {
            throw ToUserFacingException(ex);
        }
    }

    public void Create(ProspectFormViewModel model)
    {
        try
        {
            CreateInternalAsync(model).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw ToUserFacingException(ex);
        }
    }

    public void Update(ProspectFormViewModel model)
    {
        try
        {
            UpdateInternalAsync(model).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw ToUserFacingException(ex);
        }
    }

    public void UpdateChecklist(Guid id, List<ChecklistItem> checklistItems)
    {
        try
        {
            UpdateChecklistInternalAsync(id, checklistItems).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw ToUserFacingException(ex);
        }
    }

    public void Delete(Guid id)
    {
        try
        {
            DeleteInternalAsync(id).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw ToUserFacingException(ex);
        }
    }

    private async Task<List<Prospect>> GetAllInternalAsync()
    {
        var collection = GetProspectsCollection();
        if (collection is null)
        {
            return [];
        }

        var snapshot = await collection.GetSnapshotAsync();
        var prospects = snapshot.Documents.Select(MapProspect).ToList();
        await ApplySharedChecklistsAsync(prospects);
        return prospects;
    }

    private async Task<Prospect?> GetByIdInternalAsync(Guid id)
    {
        var collection = GetProspectsCollection();
        if (collection is null)
        {
            return null;
        }

        var document = await collection.Document(id.ToString()).GetSnapshotAsync();
        if (!document.Exists)
        {
            return null;
        }

        var prospect = MapProspect(document);
        await ApplySharedChecklistAsync(prospect);
        return prospect;
    }

    private async Task CreateInternalAsync(ProspectFormViewModel model)
    {
        var collection = GetProspectsCollection();
        if (collection is null)
        {
            return;
        }

        var prospect = new Prospect
        {
            Id = Guid.NewGuid(),
            OwnerUserId = _currentUserService.UserId ?? string.Empty,
            OwnerEmail = _currentUserService.Email ?? string.Empty,
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
            CreatedAt = DateTime.UtcNow
        };

        await collection.Document(prospect.Id.ToString()).SetAsync(ToFirestore(prospect));
    }

    private async Task UpdateInternalAsync(ProspectFormViewModel model)
    {
        if (model.Id is null)
        {
            return;
        }

        var collection = GetProspectsCollection();
        if (collection is null)
        {
            return;
        }

        var documentReference = collection.Document(model.Id.Value.ToString());
        var snapshot = await documentReference.GetSnapshotAsync();
        if (!snapshot.Exists)
        {
            return;
        }

        var existing = MapProspect(snapshot);
        existing.FullName = model.FullName;
        existing.Phone = model.Phone;
        existing.Email = model.Email ?? string.Empty;
        existing.City = model.City ?? string.Empty;
        existing.Source = model.Source ?? string.Empty;
        existing.Temperature = model.Temperature;
        existing.Status = model.Status;
        existing.NextContactDate = model.NextContactDate;
        existing.Notes = model.Notes ?? string.Empty;
        existing.ChecklistItems = NormalizeChecklist(model.ChecklistItems);

        await documentReference.SetAsync(ToFirestore(existing));
    }

    private async Task UpdateChecklistInternalAsync(Guid id, List<ChecklistItem> checklistItems)
    {
        var collection = GetProspectsCollection();
        if (collection is null)
        {
            return;
        }

        var documentReference = collection.Document(id.ToString());
        var snapshot = await documentReference.GetSnapshotAsync();
        if (!snapshot.Exists)
        {
            return;
        }

        var existing = MapProspect(snapshot);
        existing.ChecklistItems = NormalizeChecklist(checklistItems);
        await documentReference.UpdateAsync(new Dictionary<string, object>
        {
            ["ChecklistItems"] = existing.ChecklistItems.Select(item => new Dictionary<string, object>
            {
                ["Title"] = item.Title,
                ["IsCompleted"] = item.IsCompleted
            }).ToList()
        });

        await GetSharedChecklistCollection()
            .Document(id.ToString())
            .SetAsync(new Dictionary<string, object>
            {
                ["ChecklistItems"] = existing.ChecklistItems.Select(item => new Dictionary<string, object>
                {
                    ["Title"] = item.Title,
                    ["IsCompleted"] = item.IsCompleted
                }).ToList()
            });
    }

    private async Task DeleteInternalAsync(Guid id)
    {
        var collection = GetProspectsCollection();
        if (collection is null)
        {
            return;
        }

        var documentReference = collection.Document(id.ToString());
        var snapshot = await documentReference.GetSnapshotAsync();
        if (!snapshot.Exists)
        {
            return;
        }

        await documentReference.DeleteAsync();
    }

    private CollectionReference? GetProspectsCollection()
    {
        if (!_currentUserService.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUserService.UserId))
        {
            return null;
        }

        return _firestoreDb.Collection("users")
            .Document(_currentUserService.UserId)
            .Collection("prospects");
    }

    private CollectionReference GetSharedChecklistCollection()
        => _firestoreDb.Collection(SharedChecklistCollectionName);

    private static Prospect MapProspect(DocumentSnapshot document)
    {
        var data = document.ToDictionary();

        return new Prospect
        {
            Id = Guid.TryParse(document.Id, out var id) ? id : Guid.NewGuid(),
            OwnerUserId = data.TryGetValue("OwnerUserId", out var ownerUserId) ? ownerUserId?.ToString() ?? string.Empty : string.Empty,
            OwnerEmail = data.TryGetValue("OwnerEmail", out var ownerEmail) ? ownerEmail?.ToString() ?? string.Empty : string.Empty,
            FullName = data.TryGetValue("FullName", out var fullName) ? fullName?.ToString() ?? string.Empty : string.Empty,
            Phone = data.TryGetValue("Phone", out var phone) ? phone?.ToString() ?? string.Empty : string.Empty,
            Email = data.TryGetValue("Email", out var email) ? email?.ToString() ?? string.Empty : string.Empty,
            City = data.TryGetValue("City", out var city) ? city?.ToString() ?? string.Empty : string.Empty,
            Source = data.TryGetValue("Source", out var source) ? source?.ToString() ?? string.Empty : string.Empty,
            Status = ParseEnum<ProspectStatus>(data, "Status", ProspectStatus.Prospect),
            Temperature = ParseEnum<ProspectTemperature>(data, "Temperature", ProspectTemperature.Warm),
            NextContactDate = ParseDateTime(data, "NextContactDate", DateTime.Today.AddDays(2)),
            Notes = data.TryGetValue("Notes", out var notes) ? notes?.ToString() ?? string.Empty : string.Empty,
            ChecklistItems = ParseChecklist(data),
            CreatedAt = ParseDateTime(data, "CreatedAt", DateTime.UtcNow)
        };
    }

    private static Dictionary<string, object> ToFirestore(Prospect prospect)
    {
        return new Dictionary<string, object>
        {
            ["OwnerUserId"] = prospect.OwnerUserId,
            ["OwnerEmail"] = prospect.OwnerEmail,
            ["FullName"] = prospect.FullName,
            ["Phone"] = prospect.Phone,
            ["Email"] = prospect.Email,
            ["City"] = prospect.City,
            ["Source"] = prospect.Source,
            ["Status"] = prospect.Status.ToString(),
            ["Temperature"] = prospect.Temperature.ToString(),
            ["NextContactDate"] = Timestamp.FromDateTime(DateTime.SpecifyKind(prospect.NextContactDate, DateTimeKind.Utc)),
            ["Notes"] = prospect.Notes,
            ["ChecklistItems"] = prospect.ChecklistItems.Select(item => new Dictionary<string, object>
            {
                ["Title"] = item.Title,
                ["IsCompleted"] = item.IsCompleted
            }).ToList(),
            ["CreatedAt"] = Timestamp.FromDateTime(DateTime.SpecifyKind(prospect.CreatedAt, DateTimeKind.Utc))
        };
    }

    private async Task ApplySharedChecklistsAsync(List<Prospect> prospects)
    {
        foreach (var prospect in prospects)
        {
            await ApplySharedChecklistAsync(prospect);
        }
    }

    private async Task ApplySharedChecklistAsync(Prospect prospect)
    {
        var snapshot = await GetSharedChecklistCollection()
            .Document(prospect.Id.ToString())
            .GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            return;
        }

        var data = snapshot.ToDictionary();
        prospect.ChecklistItems = ParseChecklist(data);
    }

    private static DateTime ParseDateTime(IReadOnlyDictionary<string, object> data, string key, DateTime fallback)
    {
        if (!data.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return value switch
        {
            Timestamp timestamp => timestamp.ToDateTime().ToLocalTime(),
            DateTime dateTime => dateTime,
            _ => fallback
        };
    }

    private static TEnum ParseEnum<TEnum>(IReadOnlyDictionary<string, object> data, string key, TEnum fallback)
        where TEnum : struct, Enum
    {
        if (!data.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return Enum.TryParse<TEnum>(value.ToString(), out var result) ? result : fallback;
    }

    private static List<ChecklistItem> ParseChecklist(IReadOnlyDictionary<string, object> data)
    {
        if (!data.TryGetValue("ChecklistItems", out var value) || value is not IEnumerable<object> items)
        {
            return ChecklistDefaults.Create();
        }

        var checklist = new List<ChecklistItem>();
        foreach (var item in items)
        {
            if (item is Dictionary<string, object> dictionary)
            {
                checklist.Add(new ChecklistItem
                {
                    Title = dictionary.TryGetValue("Title", out var title) ? title?.ToString() ?? string.Empty : string.Empty,
                    IsCompleted = dictionary.TryGetValue("IsCompleted", out var completed) && completed is bool boolValue && boolValue
                });
            }
        }

        return NormalizeChecklist(checklist);
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

    private static UserFacingException ToUserFacingException(Exception ex)
    {
        var message = ex switch
        {
            AggregateException aggregate when aggregate.InnerException is not null => ToUserFacingException(aggregate.InnerException).Message,
            GoogleApiException => "Nao foi possivel acessar o Firestore agora. Tente novamente em instantes.",
            InvalidOperationException => "O Firestore nao foi configurado corretamente para este ambiente.",
            _ => "Ocorreu um erro ao acessar seus prospectos. Tente novamente."
        };

        return new UserFacingException(message, ex);
    }
}
