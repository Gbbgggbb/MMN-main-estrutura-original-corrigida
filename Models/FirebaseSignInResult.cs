namespace MMN.Web.Models;

public class FirebaseSignInResult
{
    public bool Success { get; set; }
    public string Email { get; set; } = string.Empty;
    public string LocalId { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
