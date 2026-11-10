using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Infrastructure;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (!HttpContext.Session.IsSignedIn()) return RedirectToAction("Login", "Account");

        return HttpContext.Session.IsOrganiser()
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Participant");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code)
    {
        var status = code ?? 500;
        var vm = status switch
        {
            404 => new ErrorVm { Code = 404, Title = "We can't find that page", Message = "The page may have moved, or the link may be wrong." },
            403 => new ErrorVm { Code = 403, Title = "You can't go there", Message = "Your account doesn't have access to that page." },
            _ => new ErrorVm { Code = status }
        };

        if (TempData["ErrorMessage"] is string message && !string.IsNullOrWhiteSpace(message))
            vm.Message = message;

        return View(vm);
    }
}
