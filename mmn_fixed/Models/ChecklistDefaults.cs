namespace MMN.Web.Models;

public static class ChecklistDefaults
{
    public const int MaxItems = 10;

    public static List<ChecklistItem> Create()
    {
        return
        [
            new() { Title = "Conversar com o cliente" },
            new() { Title = "Ligar para o cliente" },
            new() { Title = "Apresentar produto" }
        ];
    }
}
