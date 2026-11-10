using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Infrastructure;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class AccountController : Controller
{
    private readonly IRaceDayApi _api;

    public AccountController(IRaceDayApi api) => _api = api;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.IsSignedIn()) return RedirectToHome();
        return View(new LoginVm { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        try
        {
            var auth = await _api.LoginAsync(new LoginRequest { Email = vm.Email.Trim(), Password = vm.Password });
            if (string.IsNullOrEmpty(auth.Token))
            {
                ModelState.AddModelError("", "We couldn't log you in. Please try again.");
                return View(vm);
            }

            HttpContext.Session.SignIn(auth);

            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return LocalRedirect(vm.ReturnUrl);

            return RedirectToHome();
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 401 or 404)
        {
            ModelState.AddModelError("", "Incorrect email or password.");
            return View(vm);
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsSignedIn()) return RedirectToHome();
        return View(new RegisterVm());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (vm.Role != Roles.Organiser && vm.Role != Roles.Participant)
            ModelState.AddModelError(nameof(vm.Role), "Choose how you'll use RaceDay.");

        if (!ModelState.IsValid) return View(vm);

        try
        {
            await _api.RegisterAsync(new RegisterRequest
            {
                FullName = vm.FullName.Trim(),
                Email = vm.Email.Trim(),
                Password = vm.Password,
                Role = vm.Role
            });

            TempData["Success"] = "Your account is ready. Log in to get started.";
            return RedirectToAction(nameof(Login));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["Success"] = "You've been logged out.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToHome() =>
        HttpContext.Session.IsOrganiser()
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Participant");
}
