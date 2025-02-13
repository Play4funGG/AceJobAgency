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
            // Retrieve UserId from session to check if the user is signed in
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                // If no UserId is found in session, the user is not signed in
                return RedirectToPage("/Login");
            }

            // Fetch user details
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                // If the user is not found in the database, log them out
                HttpContext.Session.Clear();
                return RedirectToPage("/Login");
            }

            // Display user details
            ViewData["Email"] = user.Email;
            ViewData["FirstName"] = user.FirstName;
            ViewData["LastName"] = user.LastName;
            ViewData["NRIC"] = EncryptionHelper.Decrypt(user.EncryptedNRIC);

            return Page();
        }
    }
}