using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using MMN.Web.Models;

namespace MMN.Web.Services;

public class FirebaseAuthService(HttpClient httpClient, IOptions<FirebaseOptions> firebaseOptions) : IFirebaseAuthService
{
    private readonly FirebaseOptions _firebaseOptions = firebaseOptions.Value;

    public async Task<FirebaseSignInResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
        => await SendAuthRequestAsync("signInWithPassword", email, password, cancellationToken);

    public async Task<FirebaseSignInResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
        => await SendAuthRequestAsync("signUp", email, password, cancellationToken);

    private async Task<FirebaseSignInResult> SendAuthRequestAsync(string action, string email, string password, CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = $"https://identitytoolkit.googleapis.com/v1/accounts:{action}?key={_firebaseOptions.ApiKey}";
            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            using var response = await httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var signIn = await response.Content.ReadFromJsonAsync<FirebaseSignInResponse>(cancellationToken: cancellationToken);
                return new FirebaseSignInResult
                {
                    Success = signIn is not null,
                    Email = signIn?.Email ?? email,
                    LocalId = signIn?.LocalId ?? string.Empty,
                    IdToken = signIn?.IdToken ?? string.Empty,
                    ErrorMessage = signIn is null ? "Resposta invalida do Firebase." : string.Empty
                };
            }

            var errorResponse = await response.Content.ReadFromJsonAsync<FirebaseErrorResponse>(cancellationToken: cancellationToken);
            return new FirebaseSignInResult
            {
                Success = false,
                ErrorMessage = MapError(errorResponse?.Error?.Message)
            };
        }
        catch (HttpRequestException)
        {
            return new FirebaseSignInResult
            {
                Success = false,
                ErrorMessage = "Nao foi possivel conectar ao Firebase. Verifique a internet e tente novamente."
            };
        }
        catch (TaskCanceledException)
        {
            return new FirebaseSignInResult
            {
                Success = false,
                ErrorMessage = "A autenticacao demorou demais para responder. Tente novamente."
            };
        }
    }

    private static string MapError(string? code)
    {
        return code switch
        {
            "INVALID_LOGIN_CREDENTIALS" => "E-mail ou senha invalidos.",
            "EMAIL_NOT_FOUND" => "E-mail nao encontrado.",
            "INVALID_PASSWORD" => "Senha incorreta.",
            "USER_DISABLED" => "Usuario desativado.",
            "EMAIL_EXISTS" => "Ja existe uma conta com esse e-mail.",
            "WEAK_PASSWORD : Password should be at least 6 characters" => "A senha precisa ter pelo menos 6 caracteres.",
            "CONFIGURATION_NOT_FOUND" => "O Firebase Authentication ainda nao esta configurado nesse projeto. Ative o provedor de Email/Senha no console do Firebase.",
            "OPERATION_NOT_ALLOWED" => "O login por Email/Senha nao esta habilitado no Firebase. Ative esse provedor no console.",
            _ => $"Nao foi possivel autenticar agora. Codigo do Firebase: {code ?? "desconhecido"}"
        };
    }

    private sealed class FirebaseSignInResponse
    {
        public string LocalId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string IdToken { get; set; } = string.Empty;
    }

    private sealed class FirebaseErrorResponse
    {
        public FirebaseError? Error { get; set; }
    }

    private sealed class FirebaseError
    {
        public string Message { get; set; } = string.Empty;
    }
}
