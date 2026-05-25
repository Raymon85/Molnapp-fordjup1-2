using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using RecruitmentPortal.Data;
using RecruitmentPortal.HealthChecks;
using RecruitmentPortal.Middleware;
using RecruitmentPortal.Models;
using RecruitmentPortal.Repositories;
using RecruitmentPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Development = SQLite (inner loop, no Docker)
// DockerCompose / Production = SQL Server
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(connectionString));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString));
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ── Identity ─────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    // Log auth events — never log passwords or tokens
    options.Events.OnSignedIn = ctx =>
    {
        var logger = ctx.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();
        logger.LogInformation("User signed in: {User}", ctx.Principal?.Identity?.Name);
        return Task.CompletedTask;
    };
    options.Events.OnSigningOut = ctx =>
    {
        var logger = ctx.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();
        logger.LogInformation("User signed out: {User}", ctx.HttpContext.User.Identity?.Name);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToLogin = ctx =>
    {
        var logger = ctx.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Unauthenticated access to {Path}, redirecting to login",
            ctx.Request.Path);
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

// ── Azure Blob Storage ────────────────────────────────────────────────────────
var storageAccountUri = builder.Configuration["BlobStorage:AccountUri"];
if (!string.IsNullOrEmpty(storageAccountUri))
{
    // Production: use Managed Identity (no connection string in code)
    builder.Services.AddSingleton(_ =>
        new BlobServiceClient(new Uri(storageAccountUri), new DefaultAzureCredential()));
}
else
{
    // Local dev: use Azurite connection string
    var storageConn = builder.Configuration.GetConnectionString("BlobStorage")
        ?? "UseDevelopmentStorage=true";
    builder.Services.AddSingleton(_ => new BlobServiceClient(storageConn));
}
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IJobPostingRepository, JobPostingRepository>();
builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();

// ── Application Insights ──────────────────────────────────────────────────────
var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(aiConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
    {
        ConnectionString = aiConnectionString
    });
}

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddCheck<BlobStorageHealthCheck>("blob-storage");

// ── MVC + API + OpenAPI ────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((doc, ctx, ct) =>
    {
        doc.Info.Title = "CloudSoft Recruitment API";
        doc.Info.Version = "v1";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// ── Seed data ─────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider, app.Configuration);
}

// ── HTTP pipeline ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.MapOpenApi("/openapi/v1.json");
app.MapScalarApiReference("/swagger");   // available at /swagger

app.UseHttpsRedirection();
app.UseRouting();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers(); // includes HealthController at /healthz with detailed JSON

app.Run();
