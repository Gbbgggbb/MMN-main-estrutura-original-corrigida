using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMN.Web.Models;
using MMN.Web.Services;

namespace MMN.Web.Controllers;

public class AccountController(IFirebaseAuthService firebaseAuthService) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Prospects");
        }

        ViewData["Title"] = "Login";
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Prospects");
        }

        ViewData["Title"] = "Criar conta";
        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Login";
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        FirebaseSignInResult result;
        try
        {
            result = await firebaseAuthService.SignInAsync(model.Email, model.Password, cancellationToken);
        }
        catch (UserFacingException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["Title"] = "Login";
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            ViewData["Title"] = "Login";
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        await SignInWithCookieAsync(result);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Prospects");
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Criar conta";
            return View(model);
        }

        FirebaseSignInResult result;
        try
        {
            result = await firebaseAuthService.RegisterAsync(model.Email, model.Password, cancellationToken);
        }
        catch (UserFacingException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["Title"] = "Criar conta";
            return View(model);
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            ViewData["Title"] = "Criar conta";
            return View(model);
        }

        await SignInWithCookieAsync(result);
        return RedirectToAction("Index", "Prospects");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private async Task SignInWithCookieAsync(FirebaseSignInResult result)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.LocalId),
            new(ClaimTypes.Name, result.Email),
            new(ClaimTypes.Email, result.Email),
            new("firebase_id_token", result.IdToken)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            });
    }
}
