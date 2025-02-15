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
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Http;

namespace AceJobAgency.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AuthDbContext dbContext;
        private readonly GoogleReCaptchaSettings googleReCaptchaSettings;
        private readonly IEmailSender _emailSender;

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
            IOptions<GoogleReCaptchaSettings> googleReCaptchaSettings,
            IEmailSender emailSender)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.dbContext = dbContext;
            this.googleReCaptchaSettings = googleReCaptchaSettings.Value;
            this._emailSender = emailSender;
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

            // Find the user
            var user = await userManager.FindByEmailAsync(LModel.Email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid email or password.";
                return Page();
            }

            // Check if the account is locked
            if (await userManager.IsLockedOutAsync(user))
            {
                TempData["ErrorMessage"] = "Account is locked. Try again later.";
                return Page();
            }

            // Check password
            var passwordValid = await userManager.CheckPasswordAsync(user, LModel.Password);
            if (!passwordValid)
            {
                TempData["ErrorMessage"] = "Invalid email or password.";
                await LogActivity(user.Id, "Failed Login Attempt");
                return Page();
            }

            // Generate OTP (6-digit code)
            var otp = new Random().Next(100000, 999999).ToString();

            // Store OTP in TempData (valid for the next request)
            TempData["OTPCode"] = otp;
            TempData["OTPExpiry"] = DateTime.UtcNow.AddMinutes(5); // Set expiry time

            // Send OTP via email
            await SendOTPEmail(user.Email, otp);

            // Store the UserId in the session
            HttpContext.Session.SetString("UserId", user.Id);

            return RedirectToPage("OTPVerification"); // Redirect to OTP verification page
        }

        private async Task SendOTPEmail(string email, string otp)
        {
            await _emailSender.SendEmailAsync(email, "Your OTP Code",
                $"Your OTP code is: <strong>{otp}</strong>. It will expire in 5 minutes.");
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
