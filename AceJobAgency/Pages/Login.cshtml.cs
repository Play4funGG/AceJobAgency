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

            var user = await userManager.FindByEmailAsync(LModel.Email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid email or password.";
                return Page();
            }

            // Enforce maximum password age
            if (user.LastPasswordChanged.HasValue && DateTime.UtcNow - user.LastPasswordChanged.Value > TimeSpan.FromDays(90))
            {
                TempData["ErrorMessage"] = "Your password has expired. Please reset it.";
                return RedirectToPage("ResetPassword");
            }

            // Check if the account is locked
            if (await userManager.IsLockedOutAsync(user))
            {
                TempData["ErrorMessage"] = "Account is locked out due to too many failed attempts. Try again later.";
                return Page();
            }

            // Check for active sessions to prevent multiple logins
            var activeSessions = dbContext.UserSessions
                .Where(s => s.UserId == user.Id && s.LastActivity > DateTime.UtcNow.AddMinutes(-20))
                .ToList();

            if (activeSessions.Any())
            {
                foreach (var session in activeSessions)
                {
                    dbContext.UserSessions.Remove(session);
                }
                await dbContext.SaveChangesAsync();
                TempData["ErrorMessage"] = "You have been logged out from another device.";
                return Page();
            }

            // Attempt sign-in
            var result = await signInManager.PasswordSignInAsync(user, LModel.Password, LModel.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                await LogActivity(user.Id, "Successful Login");

                var sessionId = Guid.NewGuid().ToString();
                var userSession = new UserSession
                {
                    UserId = user.Id,
                    SessionId = sessionId,
                    LastActivity = DateTime.UtcNow
                };

                dbContext.UserSessions.Add(userSession);
                await dbContext.SaveChangesAsync();

                HttpContext.Session.SetString("SessionId", sessionId);
                HttpContext.Session.SetString("UserId", user.Id);

                return RedirectToPage("Index");
            }

            if (result.IsLockedOut)
            {
                TempData["ErrorMessage"] = "Account locked out due to too many failed attempts. Try again later.";
                return Page();
            }

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

            var content = await response.Content.ReadAsStringAsync();
            var recaptchaResponse = JsonSerializer.Deserialize<RecaptchaResponse>(content);

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