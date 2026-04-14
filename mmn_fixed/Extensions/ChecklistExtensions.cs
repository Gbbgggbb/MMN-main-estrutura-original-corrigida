using MMN.Web.Models;

namespace MMN.Web.Extensions;

public static class ChecklistExtensions
{
    public static int CompletedCount(this Prospect prospect)
        => (prospect.ChecklistItems ?? []).Count(item => item.IsCompleted);

    public static int TotalCount(this Prospect prospect)
        => (prospect.ChecklistItems ?? []).Count;
}
