using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models;

public class LoginVm
{
    [Required(ErrorMessage = "Enter your email address."), EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterVm
{
    [Required(ErrorMessage = "Enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Enter your email address."), EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Choose a password."), StringLength(100, MinimumLength = 8, ErrorMessage = "Use at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Repeat your password."), Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Choose how you'll use RaceDay.")]
    public string Role { get; set; } = "Participant";
}

public class EventFormVm
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Enter the event name."), StringLength(150)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Add a short description."), StringLength(2000)]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Choose the event date and time.")]
    [Display(Name = "Date and time")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Enter the location."), StringLength(200)]
    public string Location { get; set; } = "";

    [Required(ErrorMessage = "Enter the distance in km."), Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public double? DistanceKm { get; set; }

    [Required(ErrorMessage = "Choose an event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "";

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? ExistingBannerUrl { get; set; }
}

public class CategoryFormVm
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Enter a category name."), StringLength(80)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Choose a category type.")]
    public string Type { get; set; } = "Age";

    [Range(0, 120, ErrorMessage = "Enter an age between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Enter an age between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public double? DistanceKm { get; set; }
}

public class ProfileVm
{
    [Required(ErrorMessage = "Enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    public string? Email { get; set; }

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "Profile picture")]
    public IFormFile? ProfilePicture { get; set; }

    public string? ProfilePictureUrl { get; set; }
}

public class ParticipantEventsVm
{
    public List<EventDto> Events { get; set; } = new();
    public string? Search { get; set; }
    public string? EventType { get; set; }
    public string? Location { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public List<string> EventTypes { get; set; } = new();
    public List<string> Locations { get; set; } = new();
    public int TotalUpcoming { get; set; }
    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(EventType)
        || !string.IsNullOrWhiteSpace(Location) || From.HasValue || To.HasValue;
}

public class EventDetailsVm
{
    public EventDto Event { get; set; } = new();
    public EnrolmentDto? MyEnrolment { get; set; }
}

public class DashboardVm
{
    public List<EventDto> Events { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public List<EventDto> Upcoming => Events.Where(e => e.EventDate >= DateTime.Now).OrderBy(e => e.EventDate).ToList();
}

public class EventEnrolmentsVm
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultRowVm
{
    public int EnrolmentId { get; set; }
    public string? ParticipantName { get; set; }
    public string? CategoryName { get; set; }

    [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Use the format hh:mm:ss, for example 01:23:45.")]
    public string? FinishTime { get; set; }

    [Range(1, 100000, ErrorMessage = "Position must be 1 or higher.")]
    public int? Position { get; set; }
}

public class ResultsEntryVm
{
    public EventDto Event { get; set; } = new();
    public List<ResultRowVm> Rows { get; set; } = new();
    public bool EventHasEnded => Event.EventDate <= DateTime.Now;
}

public class ErrorVm
{
    public int Code { get; set; }
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "Please try again in a moment.";
}

public class CategoriesVm
{
    public EventDto Event { get; set; } = new();
    public CategoryFormVm Form { get; set; } = new();
}
