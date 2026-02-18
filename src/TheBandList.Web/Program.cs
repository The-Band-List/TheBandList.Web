using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using TheBandList.Web.Auth;
using TheBandList.Web.Components;
using TheBandList.Web.Entities.Context;
using TheBandList.Web.Service;
using TheBandList.Web.Utils;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

Env.Load(Path.Combine(builder.Environment.ContentRootPath, ".env"));

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var dbHost = builder.Configuration["DB_HOST"] ?? "localhost";
var dbPort = builder.Configuration["DB_PORT"] ?? "21555";
var dbUser = builder.Configuration["DB_USERNAME"] ?? "postgres";
var dbPass = builder.Configuration["DB_PASSWORD"] ?? "password";
var dbName = builder.Configuration["DB_NAME"] ?? "database";

var connectionString = builder.Environment.IsDevelopment()
    ? "Host=localhost;Port=21555;Username=postgres;Password=password;Database=database;Include Error Detail=true"
    : $"Host={dbHost};Port={dbPort};Username={dbUser};Password={dbPass};Database={dbName}";

builder.Services.AddDbContext<TheBandListWebDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddMemoryCache();
builder.Services.AddScoped<NiveauService>();
builder.Services.AddScoped<DifficulteFeatureService>();
builder.Services.AddScoped<ResetSelectionService>();
builder.Services.AddScoped<Chargement>();
builder.Services.AddHttpClient();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<DiscordPresenceService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DiscordPresenceService>());
builder.Services.AddDiscordAuthentication(builder.Configuration);
builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo("/var/thebandlist/keys"))
                .SetApplicationName("TheBandList.Web");
builder.Services.PostConfigure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, o =>
{
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();

var fwd = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
};
fwd.KnownIPNetworks.Clear();
fwd.KnownProxies.Clear();
app.UseForwardedHeaders(fwd);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/404");
app.UseStaticFiles();

var imagesRoot = "/var/thebandlist/keys";

#if DEBUG
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "wwwroot", "PicturesDev", "MiniaturesNiveaux")),
    RequestPath = "/MiniaturesNiveaux"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "wwwroot", "PicturesDev", "MiniaturesVideosVerification")),
    RequestPath = "/MiniaturesVideosVerification"
});
#else
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(imagesRoot, "MiniaturesNiveaux")),
    RequestPath = "/MiniaturesNiveaux"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(imagesRoot, "MiniaturesVideosVerification")),
    RequestPath = "/MiniaturesVideosVerification"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(imagesRoot, "DemonsFaces")),
    RequestPath = "/DemonsFaces"
});
#endif

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapDiscordAuthEndpoints();

app.Run();