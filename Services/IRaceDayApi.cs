using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Services;

/// <summary>Everything the MVC app needs from the Part 2 API. No database or business logic lives in MVC.</summary>
public interface IRaceDayApi
{
    // Auth
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task RegisterAsync(RegisterRequest request);

    // Events
    Task<List<EventDto>> GetEventsAsync();
    Task<List<EventDto>> GetMyEventsAsync();
    Task<EventDto> GetEventAsync(int id);
    Task SaveEventAsync(int? id, EventFormVm form);
    Task DeleteEventAsync(int id);

    // Categories
    Task AddCategoryAsync(CategoryFormVm form);
    Task DeleteCategoryAsync(int eventId, int categoryId);

    // Enrolments
    Task<List<EnrolmentDto>> GetEventEnrolmentsAsync(int eventId);
    Task EnrolAsync(int eventId, int categoryId);
    Task<List<EnrolmentDto>> GetMyEnrolmentsAsync();

    // Results
    Task PublishResultsAsync(int eventId, List<ResultEntryRequest> entries);
    Task<List<ResultDto>> GetMyResultsAsync();

    // Profile
    Task<ProfileDto> GetProfileAsync();
    Task UpdateProfileAsync(ProfileVm form);
}

public class ApiException : Exception
{
    public int StatusCode { get; }
    public ApiException(int statusCode, string message) : base(message) => StatusCode = statusCode;
}
