using AceJobAgency.Model;
using AceJobAgency.Utilities;
using AceJobAgency.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace AceJobAgency.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AuthDbContext dbContext;
        private readonly GoogleReCaptchaSettings googleReCaptchaSettings;

        [BindProperty]
        public Login LModel { get; set; }

        [BindProperty]
        public string RecaptchaToken { get; set; }

        public string SuccessMessage { get; set; }

        public void OnGet()
        {
            if (TempData["ResetSuccess"] != null)
            {
                SuccessMessage = TempData["ResetSuccess"].ToString();
            }
        }

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            AuthDbContext dbContext,
            IOptions<GoogleReCaptchaSettings> googleReCaptchaSettings)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.dbContext = dbContext;
            this.googleReCaptchaSettings = googleReCaptchaSettings.Value;
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            // Validate model state
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid input. Please check your entries.";
                return Page();
            }

            // Validate reCAPTCHA
            var isCaptchaValid = await ValidateReCaptcha(RecaptchaToken);
            if (!isCaptchaValid)
            {
                TempData["ErrorMessage"] = "reCAPTCHA validation failed. Please try again.";
                return Page();
            }

            // Validate email format
            if (!IsValidEmail(LModel.Email))
            {
                TempData["ErrorMessage"] = "Invalid email format.";
                return Page();
            }

            // Find the user by email
            var user = await userManager.FindByEmailAsync(LModel.Email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid email or password.";
                return Page();
            }

            // Check if the account is locked
            if (await userManager.IsLockedOutAsync(user))
            {
                TempData["ErrorMessage"] = "Account is locked out due to too many failed attempts. Try again later.";
                return Page();
            }

            // Attempt sign-in
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

                // Store session information
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
            return Page();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> ValidateReCaptcha(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            using var client = new HttpClient();
            var response = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={googleReCaptchaSettings.SecretKey}&response={token}",
                null);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var content = await response.Content.ReadAsStringAsync();
            var recaptchaResponse = JsonSerializer.Deserialize<RecaptchaResponse>(content);

            // Handle failed reCAPTCHA validation
            return recaptchaResponse?.success == true && recaptchaResponse.score >= 0.5;
        }

        private async Task LogActivity(string userId, string activity)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Activity = System.Net.WebUtility.HtmlEncode(activity) // Encode activity to prevent XSS
            };

            dbContext.AuditLogs.Add(auditLog);
            await dbContext.SaveChangesAsync();
        }

        private class RecaptchaResponse
        {
            public bool success { get; set; }
            public float score { get; set; }
            public string action { get; set; }
            public string challenge_ts { get; set; }
            public string hostname { get; set; }
        }
    }
}
