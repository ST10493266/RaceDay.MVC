# Part 2 API changes needed for Part 3

The brief requires Azure Blob Storage to live in the **API**. The MVC app only posts files to the API. Add the following
to your Part 2 project.

## 1. Package

```
dotnet add package Azure.Storage.Blobs
```

## 2. Blob service

```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string container);
    Task DeleteAsync(string? blobUrl, string container);
}

public class BlobStorageService : IBlobStorageService
{
    private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxBytes = 5 * 1024 * 1024;
    private readonly BlobServiceClient _client;

    public BlobStorageService(BlobServiceClient client) => _client = client;

    public async Task<string> UploadImageAsync(IFormFile file, string container)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(ext) || !file.ContentType.StartsWith("image/"))
            throw new ArgumentException("Upload a JPG, PNG or WebP image.");
        if (file.Length > MaxBytes)
            throw new ArgumentException("That image is too large. The limit is 5 MB.");

        var c = _client.GetBlobContainerClient(container);
        await c.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var blob = c.GetBlobClient($"{Guid.NewGuid()}{ext}");
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string? blobUrl, string container)
    {
        if (string.IsNullOrWhiteSpace(blobUrl)) return;
        var name = Path.GetFileName(new Uri(blobUrl).AbsolutePath);
        await _client.GetBlobContainerClient(container).DeleteBlobIfExistsAsync(name);
    }
}
```

`Program.cs`:

```csharp
builder.Services.AddSingleton(new BlobServiceClient(builder.Configuration["AzureStorage:ConnectionString"]));
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
```

Keep the connection string out of Git: `dotnet user-secrets set "AzureStorage:ConnectionString" "<value>"`,
or the environment variable `AzureStorage__ConnectionString` when deployed.

## 3. Event endpoints accept multipart

```csharp
public class EventForm
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public double DistanceKm { get; set; }
    public string EventType { get; set; } = "";
    public IFormFile? Banner { get; set; }
}

[HttpPost, Authorize(Roles = "Organiser")]
public async Task<IActionResult> Create([FromForm] EventForm form)
{
    string? url = form.Banner is { Length: > 0 }
        ? await _blob.UploadImageAsync(form.Banner, "event-banners") : null;
    // save event with BannerUrl = url, OrganiserId from the JWT
}

[HttpPut("{id}"), Authorize(Roles = "Organiser")]
public async Task<IActionResult> Update(int id, [FromForm] EventForm form)
{
    // load event, confirm the caller owns it
    if (form.Banner is { Length: > 0 })
    {
        await _blob.DeleteAsync(evt.BannerUrl, "event-banners");
        evt.BannerUrl = await _blob.UploadImageAsync(form.Banner, "event-banners");
    }
    // update remaining fields, save
}
```

Return `bannerUrl`, `categories` and `enrolmentCount` in the event DTO so the MVC app can show them.
Profile pictures work the same way: `PUT api/profile` with `[FromForm]`, container `profile-pictures`.

## 4. Database

Add a nullable `BannerUrl` column to Events and `ProfilePictureUrl` to Users. If you change the schema, update your SQL
script from Part 1 as well.

## 5. Results

`POST api/events/{id}/results` receives `[{ enrolmentId, finishTime, position }]`. `GET api/results/mine` must return
`eventName, eventDate, categoryName, finishTime, position, totalFinishers` (total finishers = count of results for that
event) so the My results page can show "47th of 312".

## 6. CORS

Not needed. The MVC server calls the API server to server, not the browser.
