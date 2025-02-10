using AceJobAgency.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

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
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Don't reveal that the user does not exist or is not confirmed
                return RedirectToPage("ForgotPasswordConfirmation");
            }

            // Generate password reset token
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Construct the callback URL
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { code },
                protocol: Request.Scheme);

            // Send the password reset email
            await _emailSender.SendEmailAsync(user.Id, "Reset Password", $"Please reset your password by <a href='{callbackUrl}'>clicking here</a>.");

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
