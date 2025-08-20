using Microsoft.AspNetCore.DataProtection;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add MVC and controllers
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();

// Load configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add session & HttpContext accessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();

// Setup cookie container for HttpClients
var cookieContainer = new CookieContainer();
var handler = new HttpClientHandler
{
    CookieContainer = cookieContainer,
    UseCookies = true
};
builder.Services.AddSingleton(cookieContainer);

// Use environment variable API_BASE_URL or fallback to localhost
var apiBaseUrl = builder.Configuration["API_BASE_URL"] ?? "https://localhost:7267";

// AdminApiClient
builder.Services.AddHttpClient<AdminApiClient>(client =>
{
    client.BaseAddress = new Uri($"{apiBaseUrl}/api/admin/");
})
.ConfigurePrimaryHttpMessageHandler(() => handler);

// CarApiClient
builder.Services.AddHttpClient<CarApiClient>(client =>
{
    client.BaseAddress = new Uri($"{apiBaseUrl}/api/cars/");
})
.ConfigurePrimaryHttpMessageHandler(() => handler);

// InquiryApiClient with factory
builder.Services.AddSingleton<InquiryApiClient>(sp =>
{
    var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = clientFactory.CreateClient("InquiryApiClient");
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    var cookieContainer = sp.GetRequiredService<CookieContainer>();
    return new InquiryApiClient(httpClient, accessor, cookieContainer);
});

// Typed HttpClient registration
builder.Services.AddHttpClient("InquiryApiClient", client =>
{
    client.BaseAddress = new Uri($"{apiBaseUrl}/api/inquiries/");
}).ConfigurePrimaryHttpMessageHandler(() => handler);
var keyPath = Path.Combine(Directory.GetCurrentDirectory(), "keys");
Directory.CreateDirectory(keyPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
    .SetApplicationName("EliteCarsShared"); // must match API

var app = builder.Build();

// Middleware
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

// MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Login}/{id?}");

// API routes
app.MapControllers();

app.Run();
