using AceJobAgency.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AceJobAgency.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public ResetPasswordInput Input { get; set; }

        public class ResetPasswordInput
        {
            [Required]
            public string UserId { get; set; } // Bound to hidden field

            [Required]
            public string Code { get; set; } // Bound to hidden field

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet(string userId, string code)
        {
            if (code != null)
            {
                code = System.Text.Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }

            Input = new ResetPasswordInput
            {
                UserId = userId,
                Code = code
            };
        }


        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine($"UserId: {Input.UserId}");
            Console.WriteLine($"Reset Code: {Input.Code}");
            Console.WriteLine($"Password: {Input.Password}");
            Console.WriteLine($"ConfirmPassword: {Input.ConfirmPassword}");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.UserId);
            if (user == null)
            {
                return RedirectToPage("ForgotPasswordConfirmation");
            }

            // Reset the password
            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                TempData["ResetSuccess"] = "Your password has been successfully reset. Please log in.";
                return RedirectToPage("Login");
            }

            // Show errors if password reset fails
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}
