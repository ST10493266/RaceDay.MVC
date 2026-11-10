namespace RaceDay.Mvc.Models;

// These classes mirror the JSON returned by the Part 2 API.
// If your API uses different property names, change them here (matching is case-insensitive).

public class AuthResponse
{
    public string Token { get; set; } = "";
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public string? Type { get; set; }          // "Age" or "Distance"
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public double? DistanceKm { get; set; }

    public string Describe()
    {
        if (string.Equals(Type, "Distance", StringComparison.OrdinalIgnoreCase) && DistanceKm.HasValue)
            return $"{DistanceKm.Value:0.##} km";
        if (MinAge.HasValue && MaxAge.HasValue) return $"Ages {MinAge}-{MaxAge}";
        if (MinAge.HasValue) return $"Ages {MinAge}+";
        if (MaxAge.HasValue) return $"Up to age {MaxAge}";
        return Type ?? "";
    }
}

public class EventDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public double DistanceKm { get; set; }
    public string EventType { get; set; } = "";
    public string? BannerUrl { get; set; }
    public int OrganiserId { get; set; }
    public string? OrganiserName { get; set; }
    public int EnrolmentCount { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
}

public class EnrolmentDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string? EventName { get; set; }
    public DateTime EventDate { get; set; }
    public int ParticipantId { get; set; }
    public string? ParticipantName { get; set; }
    public string? ParticipantEmail { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? EnrolledAt { get; set; }
}

public class ResultDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string? CategoryName { get; set; }
    public string FinishTime { get; set; } = "";   // e.g. "01:23:45"
    public int Position { get; set; }
    public int TotalFinishers { get; set; }
}

public class ProfileDto
{
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

// Requests sent to the API
public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "Participant";
}

public class EnrolRequest
{
    public int EventId { get; set; }
    public int CategoryId { get; set; }
}

public class ResultEntryRequest
{
    public int EnrolmentId { get; set; }
    public string FinishTime { get; set; } = "";
    public int Position { get; set; }
}
