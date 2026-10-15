using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Services;
using S379658_HIT339_Assignment2_Library.Users.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Use the Azure App Service environment setting ConnectionStrings__DefaultConnection in production.
// Keep localdb only for the Development launch profiles.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString) ||
    (!builder.Environment.IsDevelopment() &&
     connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase)))
{
    throw new InvalidOperationException(
        "Configure ConnectionStrings__DefaultConnection to a cloud SQL Server before starting outside Development.");
}
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// Honour reverse-proxy headers ONLY from explicitly configured trusted proxies.
// Azure Windows/IIS hosting normally supplies the correct request scheme automatically.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var address in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>()
                 ?? Array.Empty<string>())
    {
        if (!IPAddress.TryParse(address, out var proxy))
            throw new InvalidOperationException($"Invalid trusted reverse proxy address: {address}");
        options.KnownProxies.Add(proxy);
    }
});

// ASP.NET Core Identity now owns users, password hashing, roles, claims and sign-in cookies.
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        // The existing seeded passwords satisfy these defaults. Keeping the settings explicit
        // makes the assignment's password expectations easy to see and maintain.
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = false;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // Use the stable pre-passkey Identity schema for this password/role-based MVC app.
        // The explicit key length keeps the EF model and included migration deterministic.
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version2;
        options.Stores.MaxLengthForKeys = 128;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Separate limits by remote IP so one visitor does not consume everyone's quota.
    options.AddPolicy("public-api", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    options.AddPolicy("borrower-register", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromHours(1),
            QueueLimit = 0
        }));
    options.AddPolicy("kiosk-login", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    // Keep the project's existing custom login and access-denied pages.
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "LibraryAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

builder.Services.AddScoped<FineService>();
builder.Services.AddScoped<BorrowerAccountService>();
builder.Services.AddScoped<KioskCheckoutService>();
builder.Services.AddScoped<LibraryCheckoutService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<NotificationSweepService>();
builder.Services.AddScoped<TransferService>();
// Background scanning is enabled on the single web host by default.
// Disable on extra instances or run it in a dedicated always-on worker when scaling out.
if (builder.Configuration.GetValue("LibraryHosting:RunNotificationWorker", true))
    builder.Services.AddHostedService<LibraryNotificationWorker>();
builder.Services.AddScoped<ManagerStatisticsService>();
builder.Services.AddScoped<MediaImportService>();
builder.Services.AddHttpClient<ExternalMetadataService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(12);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddScoped<ManagerReportService>();
builder.Services.AddScoped<ReportExportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(); // Must precede HTTPS redirection/authentication.
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // Attribute-routed read-only JSON endpoints.

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Apply EF Core migrations and create the starter Identity staff accounts/roles.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    if (app.Environment.IsDevelopment())
    {
        // Local demo only: automatic migrations and the seeded demo staff/borrower accounts.
        await DatabaseSeeder.SeedAsync(context, userManager, roleManager, includeDemoBorrower: true);
    }
    else
    {
        // Production migrations must be applied explicitly during deployment.
        // Only create an initial admin if one-time bootstrap credentials are configured.
        await DatabaseSeeder.SeedProductionAsync(
            userManager, roleManager,
            app.Configuration["LibraryBootstrap:AdminUsername"],
            app.Configuration["LibraryBootstrap:AdminPassword"],
            app.Configuration["LibraryBootstrap:AdminEmail"]);
        // Optional one-time demonstration data for an explicitly configured Azure deployment.
        // This never uses the known local Development passwords.
        await DatabaseSeeder.SeedConfiguredProductionDemoAsync(
            context, userManager, app.Configuration,
            services.GetRequiredService<ILoggerFactory>().CreateLogger("LibraryDemoSeed"));
    }
}

app.Run();

// Makes Program visible to integration test projects without changing runtime behaviour.
public partial class Program { }
