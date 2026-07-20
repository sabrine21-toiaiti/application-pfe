using SebnWeb.Data;
using SebnWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Cloud platforms (Render, Railway, Azure...) fournissent le port via la variable
// d'environnement PORT. En local, on garde le port par défaut (5000/launchSettings).
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddRazorPages();
builder.Services.AddSingleton<AppDataStore>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
});

builder.Services.AddHttpClient<DetectionApiClient>(client =>
{
    var baseUrl = builder.Configuration["DetectionApi:BaseUrl"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
