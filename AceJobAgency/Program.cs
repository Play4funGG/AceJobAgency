using AceJobAgency.Model;
using AceJobAgency.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute());
});

builder.Services.AddTransient<IEmailSender, AceJobAgency.Utilities.EmailSender>();

// Add DbContext with AuthDbContext
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthConnectionString")));

// Add Identity with ApplicationUser and AuthDbContext
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password complexity requirements
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;

    //// 2FA settings
    //options.SignIn.RequireConfirmedAccount = true; // Require email confirmation
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

// Add Session Services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HttpContextAccessor (for using sessions, etc.)
builder.Services.AddHttpContextAccessor();

// Google reCAPTCHA settings from configuration
builder.Services.Configure<GoogleReCaptchaSettings>(builder.Configuration.GetSection("GoogleReCaptcha"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Global error handling
    app.UseExceptionHandler("/Errors/Error");

    // HSTS for secure connections
    app.UseHsts();
}

// Ensure the uploads directory exists
var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
if (!Directory.Exists(uploadsFolder))
{
    Directory.CreateDirectory(uploadsFolder);
}

// Redirect root URL ("/") based on authentication status
app.MapGet("/", async context =>
{
    var user = context.User;
    if (user.Identity?.IsAuthenticated == true)
    {
        // Redirect authenticated users to the homepage
        context.Response.Redirect("/Index");
    }
    else
    {
        // Redirect unauthenticated users to the login page
        context.Response.Redirect("/Login");
    }

    await Task.CompletedTask;
});

// Middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable Session Middleware
app.UseSession();

// Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Error handling middleware
app.UseStatusCodePagesWithReExecute("/Errors/{0}");

// Map Razor Pages
app.MapRazorPages();

app.Run();
