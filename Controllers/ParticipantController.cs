using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Infrastructure;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize(Role = Roles.Participant)]
public class ParticipantController : Controller
{
    private readonly IRaceDayApi _api;

    public ParticipantController(IRaceDayApi api) => _api = api;

    // Home: upcoming events with filters
    public async Task<IActionResult> Index(string? search, string? eventType, string? location, DateTime? from, DateTime? to)
    {
        var upcoming = (await _api.GetEventsAsync())
            .Where(e => e.EventDate >= DateTime.Now)
            .OrderBy(e => e.EventDate)
            .ToList();

        var filtered = upcoming.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(e =>
                Contains(e.Name, term) || Contains(e.Description, term) ||
                Contains(e.Location, term) || Contains(e.EventType, term));
        }
        if (!string.IsNullOrWhiteSpace(eventType))
            filtered = filtered.Where(e => string.Equals(e.EventType, eventType, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(location))
            filtered = filtered.Where(e => string.Equals(e.Location, location, StringComparison.OrdinalIgnoreCase));
        if (from.HasValue) filtered = filtered.Where(e => e.EventDate.Date >= from.Value.Date);
        if (to.HasValue) filtered = filtered.Where(e => e.EventDate.Date <= to.Value.Date);

        var vm = new ParticipantEventsVm
        {
            Events = filtered.ToList(),
            Search = search,
            EventType = eventType,
            Location = location,
            From = from,
            To = to,
            TotalUpcoming = upcoming.Count,
            EventTypes = upcoming.Select(e => e.EventType).Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList(),
            Locations = upcoming.Select(e => e.Location).Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList()
        };
        return View(vm);
    }

    // Full event information and categories
    public async Task<IActionResult> Details(int id)
    {
        var evt = await _api.GetEventAsync(id);

        EnrolmentDto? mine = null;
        try
        {
            mine = (await _api.GetMyEnrolmentsAsync())
                .FirstOrDefault(e => e.EventId == id && !IsCancelled(e.Status));
        }
        catch (ApiException ex) when (ex.StatusCode is not (401 or 403)) { /* the page still works without this */ }

        return View(new EventDetailsVm { Event = evt, MyEnrolment = mine });
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (categoryId <= 0)
        {
            TempData["Error"] = "Choose a category before you enrol.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        try
        {
            await _api.EnrolAsync(eventId, categoryId);
            TempData["Success"] = "You're enrolled. Your enrolment is listed below.";
            return RedirectToAction(nameof(MyEnrolments));
        }
        catch (ApiException ex) when (ex.StatusCode is not (401 or 403))
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id = eventId });
        }
    }

    public async Task<IActionResult> MyEnrolments()
    {
        var list = await _api.GetMyEnrolmentsAsync();
        return View(list.OrderBy(e => e.EventDate < DateTime.Now).ThenBy(e => e.EventDate).ToList());
    }

    public async Task<IActionResult> MyResults()
    {
        var list = await _api.GetMyResultsAsync();
        return View(list.OrderByDescending(r => r.EventDate).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var p = await _api.GetProfileAsync();
        return View(new ProfileVm
        {
            FullName = p.FullName,
            Email = p.Email,
            PhoneNumber = p.PhoneNumber,
            DateOfBirth = p.DateOfBirth,
            ProfilePictureUrl = p.ProfilePictureUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileVm vm)
    {
        var imageError = ImageRules.Validate(vm.ProfilePicture);
        if (imageError != null) ModelState.AddModelError(nameof(vm.ProfilePicture), imageError);

        if (!ModelState.IsValid) return View(vm);

        try
        {
            await _api.UpdateProfileAsync(vm);
            HttpContext.Session.SetString(SessionKeys.FullName, vm.FullName.Trim());
            TempData["Success"] = "Your profile is updated.";
            return RedirectToAction(nameof(Profile));
        }
        catch (ApiException ex) when (ex.StatusCode is not (401 or 403))
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
    }

    private static bool Contains(string? source, string term) =>
        source?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsCancelled(string? status) =>
        string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "Canceled", StringComparison.OrdinalIgnoreCase);
}
