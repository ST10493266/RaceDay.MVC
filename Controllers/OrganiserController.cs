using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Infrastructure;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize(Role = Roles.Organiser)]
public class OrganiserController : Controller
{
    private readonly IRaceDayApi _api;

    public OrganiserController(IRaceDayApi api) => _api = api;

    // ---------- Dashboard ----------
    public async Task<IActionResult> Dashboard()
    {
        var events = await _api.GetMyEventsAsync();
        return View(new DashboardVm { Events = events });
    }

    // ---------- Events ----------
    public async Task<IActionResult> Events()
    {
        var events = await _api.GetMyEventsAsync();
        return View(events.OrderByDescending(e => e.EventDate).ToList());
    }

    [HttpGet]
    public IActionResult CreateEvent() =>
        View("EventForm", new EventFormVm { EventDate = DateTime.Today.AddDays(30).AddHours(7) });

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormVm vm)
    {
        var bannerError = ImageRules.Validate(vm.Banner);
        if (bannerError != null) ModelState.AddModelError(nameof(vm.Banner), bannerError);
        if (!ModelState.IsValid) return View("EventForm", vm);

        try
        {
            await _api.SaveEventAsync(null, vm);
            TempData["Success"] = "Event created. Add its categories next.";
            return RedirectToAction(nameof(Events));
        }
        catch (ApiException ex) when (ex.StatusCode is not (401 or 403))
        {
            ModelState.AddModelError("", ex.Message);
            return View("EventForm", vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var e = await _api.GetEventAsync(id);
        return View("EventForm", new EventFormVm
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description ?? "",
            EventDate = e.EventDate,
            Location = e.Location,
            DistanceKm = e.DistanceKm,
            EventType = e.EventType,
            ExistingBannerUrl = e.BannerUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(int id, EventFormVm vm)
    {
        vm.Id = id;
        var bannerError = ImageRules.Validate(vm.Banner);
        if (bannerError != null) ModelState.AddModelError(nameof(vm.Banner), bannerError);
        if (!ModelState.IsValid) return View("EventForm", vm);

        try
        {
            await _api.SaveEventAsync(id, vm);
            TempData["Success"] = "Event saved.";
            return RedirectToAction(nameof(Events));
        }
        catch (ApiException ex) when (ex.StatusCode is not (401 or 403))
        {
            ModelState.AddModelError("", ex.Message);
            return View("EventForm", vm);
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        try
        {
            await _api.DeleteEventAsync(id);
            TempData["Success"] = "Event deleted.";
        }
        catch (ApiException ex) when (ex.StatusCode is not (401 or 403))
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Events));
    }

    // ---------- Categories ----------
    [HttpGet]
    public async Task<IActionResult> Categories(int eventId)
    {
        var e = await _api.GetEventAsync(eventId);
        return View(new CategoriesVm { Event = e, Form = new CategoryFormVm { EventId = eventId } });
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory([Bind(Prefix = "Form")] CategoryFormVm form)
    {
        if (form.Type == "Age")
        {
            if (form.MinAge is null && form.MaxAge is null)
                ModelState.AddModelError($"Form.{nameof(form.MinAge)}", "Enter a minimum age, a maximum age, or both.");
            else if (form.MinAge > form.MaxAge)
                ModelState.AddModelError($"Form.{nameof(form.MaxAge)}", "Maximum age must be higher than minimum age.");
        }
        else if (form.Type == "Distance")
        {
            if (form.DistanceKm is null)
                ModelState.AddModelError($"Form.{nameof(form.DistanceKm)}", "Enter the distance in km.");
        }
        else
        {
            ModelState.AddModelError($"Form.{nameof(form.Type)}", "Choose a category type.");
        }

        if (!ModelState.IsValid)
        {
            var e = await _api.GetEventAsync(form.EventId);
            return View("Categories", new CategoriesVm { Event = e, Form = form });
        }

        try
        {
            await _api.AddCategoryAsync(form);
            TempData["Success"] = "Category added.";
        }
        catch (ApiException ex) when (ex.StatusCode is not (401 or 403))
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Categories), new { eventId = form.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
    {
        try
        {
            await _api.DeleteCategoryAsync(eventId, categoryId);
            TempData["Success"] = "Category removed.";
        }
        catch (ApiException ex) when (ex.StatusCode is not (401 or 403))
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ---------- Enrolments ----------
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var e = await _api.GetEventAsync(eventId);
        var list = await _api.GetEventEnrolmentsAsync(eventId);
        return View(new EventEnrolmentsVm { Event = e, Enrolments = list.OrderBy(x => x.ParticipantName).ToList() });
    }

    // ---------- Results ----------
    [HttpGet]
    public async Task<IActionResult> Results(int eventId)
    {
        var vm = await BuildResultsVmAsync(eventId);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Results(int eventId, List<ResultRowVm> rows)
    {
        var vm = await BuildResultsVmAsync(eventId);

        if (!vm.EventHasEnded)
        {
            TempData["Error"] = "Results can only be published after the event has finished.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        // Keep what the organiser typed, in the order of the enrolments.
        foreach (var row in vm.Rows)
        {
            var posted = rows.FirstOrDefault(r => r.EnrolmentId == row.EnrolmentId);
            if (posted is not null)
            {
                row.FinishTime = posted.FinishTime;
                row.Position = posted.Position;
            }
        }

        var entered = vm.Rows.Where(r => !string.IsNullOrWhiteSpace(r.FinishTime) || r.Position.HasValue).ToList();

        for (var i = 0; i < vm.Rows.Count; i++)
        {
            var row = vm.Rows[i];
            var any = !string.IsNullOrWhiteSpace(row.FinishTime) || row.Position.HasValue;
            if (any && string.IsNullOrWhiteSpace(row.FinishTime))
                ModelState.AddModelError($"rows[{i}].FinishTime", "Enter a finish time.");
            if (any && !row.Position.HasValue)
                ModelState.AddModelError($"rows[{i}].Position", "Enter a finishing position.");
        }

        var duplicate = entered.Where(r => r.Position.HasValue).GroupBy(r => r.Position!.Value).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            ModelState.AddModelError("", $"Position {duplicate.Key} is used more than once. Each finishing position must be unique.");

        if (entered.Count == 0)
            ModelState.AddModelError("", "Enter at least one result to publish.");

        if (!ModelState.IsValid) return View(vm);

        try
        {
            await _api.PublishResultsAsync(eventId, entered.Select(r => new ResultEntryRequest
            {
                EnrolmentId = r.EnrolmentId,
                FinishTime = r.FinishTime!.Trim(),
                Position = r.Position!.Value
            }).ToList());

            TempData["Success"] = "Results published. Participants can now see them under My results.";
            return RedirectToAction(nameof(Results), new { eventId });
        }
        catch (ApiException ex) when (ex.StatusCode is not (401 or 403))
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
    }

    private async Task<ResultsEntryVm> BuildResultsVmAsync(int eventId)
    {
        var e = await _api.GetEventAsync(eventId);
        var enrolments = await _api.GetEventEnrolmentsAsync(eventId);

        return new ResultsEntryVm
        {
            Event = e,
            Rows = enrolments
                .Where(x => !string.Equals(x.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(x.Status, "Canceled", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.ParticipantName)
                .Select(x => new ResultRowVm
                {
                    EnrolmentId = x.Id,
                    ParticipantName = x.ParticipantName,
                    CategoryName = x.CategoryName
                }).ToList()
        };
    }
}
