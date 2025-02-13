using AceJobAgency.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Encodings.Web;

namespace AceJobAgency.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        [BindProperty]
        public ForgotPasswordInput Input { get; set; }

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToPage("ForgotPasswordConfirmation");
            }

            // Generate password reset token
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            if (string.IsNullOrEmpty(code))
            {
                Console.WriteLine("Error: Generated reset token is null or empty.");
                return RedirectToPage("/Errors/Error");
            }

            var encodedCode = WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(code));

            // Construct the callback URL properly with UserId and Code as query parameters
            var callbackUrl = Url.Page(
                "/ResetPassword",
                pageHandler: null,
                values: new { userId = user.Id, code = encodedCode },
                protocol: Request.Scheme);

            if (string.IsNullOrEmpty(callbackUrl))
            {
                Console.WriteLine("Error: Generated callbackUrl is null or empty.");
                return RedirectToPage("Error");
            }

            // Send the password reset email with proper HTML
            await _emailSender.SendEmailAsync(user.Email, "Reset Password",
                $"<p>Please reset your password by <a href=\"{HtmlEncoder.Default.Encode(callbackUrl)}\">clicking here</a>.</p>");

            return RedirectToPage("ForgotPasswordConfirmation");
        }

        public class ForgotPasswordInput
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }
    }
}
