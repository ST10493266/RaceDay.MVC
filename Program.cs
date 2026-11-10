using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Infrastructure;
using RaceDay.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<ApiExceptionFilter>();
});

// Session holds the JWT, user id, name and role between requests.
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Typed client for the Part 2 API. The MVC app never touches the database or Azure.
// Api:BaseUrl comes from appsettings.json locally and from the Api__BaseUrl env var in Docker.
var apiBase = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5000/";
if (!apiBase.EndsWith('/')) apiBase += "/";

builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddHttpClient<IRaceDayApi, RaceDayApi>(client =>
{
    client.BaseAddress = new Uri(apiBase);
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<BearerTokenHandler>();

var app = builder.Build();

// Never show raw exceptions to users, in any environment.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
