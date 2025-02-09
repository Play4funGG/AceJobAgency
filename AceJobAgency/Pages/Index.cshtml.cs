using AceJobAgency.Model;
using AceJobAgency.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgency.Pages
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AuthDbContext dbContext;

        public ApplicationUser User { get; set; }

        public IndexModel(UserManager<ApplicationUser> userManager, AuthDbContext dbContext)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if the session exists
            var userId = HttpContext.Session.GetString("UserId");
            var sessionId = HttpContext.Session.GetString("SessionId");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId))
            {
                // Redirect to login page if session is invalid or expired
                return RedirectToPage("/Login");
            }

            // Validate session against the database
            var userSession = await dbContext.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);

            if (userSession == null || userSession.LastActivity < DateTime.UtcNow.AddMinutes(-20))
            {
                // Clear session and redirect to login page
                HttpContext.Session.Clear();
                return RedirectToPage("/Login");
            }

            // Update last activity time
            userSession.LastActivity = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            // Fetch user details
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                // Redirect to login page if user is not found
                return RedirectToPage("/Login");
            }

            // Decrypt NRIC
            ViewData["DecryptedNRIC"] = EncryptionHelper.Decrypt(user.EncryptedNRIC);

            return Page();
        }
    }
}