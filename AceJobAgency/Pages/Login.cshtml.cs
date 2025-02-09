using AceJobAgency.Model;
using AceJobAgency.Utilities;
using AceJobAgency.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace AceJobAgency.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AuthDbContext dbContext;

        [BindProperty]
        public Login LModel { get; set; }

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            AuthDbContext dbContext)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.dbContext = dbContext;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByEmailAsync(LModel.Email);

                if (user == null || await userManager.IsLockedOutAsync(user))
                {
                    TempData["ErrorMessage"] = "Account is locked or invalid credentials.";
                    return Page();
                }

                // Check for active sessions to prevent multiple logins
                var activeSessions = dbContext.UserSessions
                    .Where(s => s.UserId == user.Id && s.LastActivity > DateTime.UtcNow.AddMinutes(-20))
                    .ToList();

                if (activeSessions.Any())
                {
                    // Invalidate all active sessions
                    foreach (var session in activeSessions)
                    {
                        dbContext.UserSessions.Remove(session);
                    }
                    await dbContext.SaveChangesAsync();

                    TempData["ErrorMessage"] = "You have been logged out from another device.";
                    return Page();
                }

                var result = await signInManager.PasswordSignInAsync(user, LModel.Password, LModel.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    // Log successful login activity
                    await LogActivity(user.Id, "Successful Login");

                    // Create a new session
                    var sessionId = Guid.NewGuid().ToString();
                    var userSession = new UserSession
                    {
                        UserId = user.Id,
                        SessionId = sessionId,
                        LastActivity = DateTime.UtcNow
                    };

                    dbContext.UserSessions.Add(userSession);
                    await dbContext.SaveChangesAsync();

                    // Store session ID in the session
                    HttpContext.Session.SetString("SessionId", sessionId);
                    HttpContext.Session.SetString("UserId", user.Id);

                    return RedirectToPage("Index");
                }

                if (result.IsLockedOut)
                {
                    TempData["ErrorMessage"] = "Account locked out due to too many failed attempts. Try again later.";
                    return Page();
                }

                // Log failed login attempt
                await LogActivity(user.Id, "Failed Login Attempt");
                TempData["ErrorMessage"] = "Invalid email or password.";
            }

            return Page();
        }

        private async Task LogActivity(string userId, string activity)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Activity = activity
            };

            dbContext.AuditLogs.Add(auditLog);
            await dbContext.SaveChangesAsync();
        }
    }
}