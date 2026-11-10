using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Services;

/// <summary>
/// Single place that knows the API's URLs. If your Part 2 routes differ, change them here only.
/// </summary>
public static class ApiRoutes
{
    public const string Login = "api/auth/login";
    public const string Register = "api/auth/register";

    public const string Events = "api/events";                              // GET all, POST create (multipart)
    public const string MyEvents = "api/events/mine";                       // GET organiser's own events
    public static string Event(int id) => $"api/events/{id}";               // GET, PUT (multipart), DELETE
    public static string Categories(int eventId) => $"api/events/{eventId}/categories";                   // POST
    public static string Category(int eventId, int id) => $"api/events/{eventId}/categories/{id}";       // DELETE
    public static string EventEnrolments(int eventId) => $"api/events/{eventId}/enrolments";             // GET (organiser)
    public static string PublishResults(int eventId) => $"api/events/{eventId}/results";                 // POST

    public const string Enrolments = "api/enrolments";                      // POST {eventId, categoryId}
    public const string MyEnrolments = "api/enrolments/mine";               // GET
    public const string MyResults = "api/results/mine";                     // GET
    public const string Profile = "api/profile";                            // GET, PUT (multipart)
}

public class RaceDayApi : IRaceDayApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public RaceDayApi(HttpClient http) => _http = http;

    // ---------- Auth ----------
    public Task<AuthResponse> LoginAsync(LoginRequest r) => PostJsonAsync<AuthResponse>(ApiRoutes.Login, r);
    public Task RegisterAsync(RegisterRequest r) => SendAsync(HttpMethod.Post, ApiRoutes.Register, JsonBody(r));

    // ---------- Events ----------
    public Task<List<EventDto>> GetEventsAsync() => GetAsync<List<EventDto>>(ApiRoutes.Events);
    public Task<List<EventDto>> GetMyEventsAsync() => GetAsync<List<EventDto>>(ApiRoutes.MyEvents);
    public Task<EventDto> GetEventAsync(int id) => GetAsync<EventDto>(ApiRoutes.Event(id));

    public Task SaveEventAsync(int? id, EventFormVm f)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(f.Name), "Name" },
            { new StringContent(f.Description ?? ""), "Description" },
            { new StringContent(f.EventDate!.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)), "EventDate" },
            { new StringContent(f.Location), "Location" },
            { new StringContent(f.DistanceKm!.Value.ToString(CultureInfo.InvariantCulture)), "DistanceKm" },
            { new StringContent(f.EventType), "EventType" }
        };
        AddFile(form, f.Banner, "Banner");

        return id is null
            ? SendAsync(HttpMethod.Post, ApiRoutes.Events, form)
            : SendAsync(HttpMethod.Put, ApiRoutes.Event(id.Value), form);
    }

    public Task DeleteEventAsync(int id) => SendAsync(HttpMethod.Delete, ApiRoutes.Event(id));

    // ---------- Categories ----------
    public Task AddCategoryAsync(CategoryFormVm f) =>
        SendAsync(HttpMethod.Post, ApiRoutes.Categories(f.EventId), JsonBody(new
        {
            f.Name,
            f.Type,
            MinAge = f.Type == "Age" ? f.MinAge : null,
            MaxAge = f.Type == "Age" ? f.MaxAge : null,
            DistanceKm = f.Type == "Distance" ? f.DistanceKm : null
        }));

    public Task DeleteCategoryAsync(int eventId, int categoryId) =>
        SendAsync(HttpMethod.Delete, ApiRoutes.Category(eventId, categoryId));

    // ---------- Enrolments ----------
    public Task<List<EnrolmentDto>> GetEventEnrolmentsAsync(int eventId) =>
        GetAsync<List<EnrolmentDto>>(ApiRoutes.EventEnrolments(eventId));

    public Task EnrolAsync(int eventId, int categoryId) =>
        SendAsync(HttpMethod.Post, ApiRoutes.Enrolments, JsonBody(new EnrolRequest { EventId = eventId, CategoryId = categoryId }));

    public Task<List<EnrolmentDto>> GetMyEnrolmentsAsync() => GetAsync<List<EnrolmentDto>>(ApiRoutes.MyEnrolments);

    // ---------- Results ----------
    public Task PublishResultsAsync(int eventId, List<ResultEntryRequest> entries) =>
        SendAsync(HttpMethod.Post, ApiRoutes.PublishResults(eventId), JsonBody(entries));

    public Task<List<ResultDto>> GetMyResultsAsync() => GetAsync<List<ResultDto>>(ApiRoutes.MyResults);

    // ---------- Profile ----------
    public Task<ProfileDto> GetProfileAsync() => GetAsync<ProfileDto>(ApiRoutes.Profile);

    public Task UpdateProfileAsync(ProfileVm f)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(f.FullName), "FullName" },
            { new StringContent(f.PhoneNumber ?? ""), "PhoneNumber" }
        };
        if (f.DateOfBirth.HasValue)
            form.Add(new StringContent(f.DateOfBirth.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), "DateOfBirth");
        AddFile(form, f.ProfilePicture, "ProfilePicture");

        return SendAsync(HttpMethod.Put, ApiRoutes.Profile, form);
    }

    // ---------- HTTP plumbing ----------
    private static void AddFile(MultipartFormDataContent form, IFormFile? file, string field)
    {
        if (file is not { Length: > 0 }) return;
        var content = new StreamContent(file.OpenReadStream());
        content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(content, field, Path.GetFileName(file.FileName));
    }

    private static StringContent JsonBody(object body) =>
        new(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");

    private async Task<T> GetAsync<T>(string url) => Deserialise<T>(await SendAsync(HttpMethod.Get, url));

    private async Task<T> PostJsonAsync<T>(string url, object body) =>
        Deserialise<T>(await SendAsync(HttpMethod.Post, url, JsonBody(body)));

    private static T Deserialise<T>(string body)
    {
        try
        {
            var value = JsonSerializer.Deserialize<T>(body, Json);
            if (value is not null) return value;
        }
        catch (JsonException) { /* fall through */ }
        throw new ApiException(502, "The RaceDay service sent back something we couldn't read. Please try again.");
    }

    private async Task<string> SendAsync(HttpMethod method, string url, HttpContent? content = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };
            using var response = await _http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new ApiException((int)response.StatusCode, FriendlyMessage(body, (int)response.StatusCode));

            return body;
        }
        catch (HttpRequestException)
        {
            throw new ApiException(503, "We couldn't reach the RaceDay service. Please try again shortly.");
        }
        catch (TaskCanceledException)
        {
            throw new ApiException(504, "The RaceDay service took too long to respond. Please try again.");
        }
    }

    /// <summary>
    /// Only validation-style messages (400/409/422) are passed through; everything else gets a safe generic message
    /// so users never see stack traces or raw server errors.
    /// </summary>
    private static string FriendlyMessage(string body, int status)
    {
        var generic = status switch
        {
            400 => "That request wasn't valid. Check your details and try again.",
            401 => "Your session has expired. Please log in again.",
            403 => "You don't have permission to do that.",
            404 => "We couldn't find what you were looking for.",
            409 => "That clashes with something that already exists.",
            _ => "Something went wrong on our side. Please try again."
        };

        if (status is not (400 or 409 or 422) || string.IsNullOrWhiteSpace(body)) return generic;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.String) return root.GetString() ?? generic;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                    return m.GetString() ?? generic;

                if (root.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Object)
                {
                    var messages = e.EnumerateObject().SelectMany(p => Flatten(p.Value))
                        .Where(s => !string.IsNullOrWhiteSpace(s)).Take(3);
                    var joined = string.Join(" ", messages);
                    if (joined.Length > 0) return joined;
                }

                if (root.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String)
                    return d.GetString() ?? generic;
            }
        }
        catch (JsonException)
        {
            var text = body.Trim();
            if (text.Length is > 0 and <= 200 && !text.StartsWith('<')) return text;
        }

        return generic;
    }

    private static IEnumerable<string?> Flatten(JsonElement value) =>
        value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : null)
            : new[] { value.ValueKind == JsonValueKind.String ? value.GetString() : null };
}
