using MMN.Web.Models;

namespace MMN.Web.Services;

public interface IFirebaseAuthService
{
    Task<FirebaseSignInResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<FirebaseSignInResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default);
}
