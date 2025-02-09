using AceJobAgency.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgency.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly AuthDbContext dbContext;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, AuthDbContext dbContext)
        {
            this.signInManager = signInManager;
            this.dbContext = dbContext;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var sessionId = HttpContext.Session.GetString("SessionId");

            if (!string.IsNullOrEmpty(sessionId))
            {
                var userSession = await dbContext.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (userSession != null)
                {
                    dbContext.UserSessions.Remove(userSession);
                    await dbContext.SaveChangesAsync();
                }
            }

            await signInManager.SignOutAsync();
            return RedirectToPage("/Login");
        }
    }
}