using AceJobAgency.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AceJobAgency.Pages
{
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly AuthDbContext dbContext;

        [BindProperty]
        public ChangePasswordInput Input { get; set; }

        public ChangePasswordModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AuthDbContext dbContext)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.dbContext = dbContext;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            // Enforce minimum password age
            if (user.LastPasswordChanged.HasValue && DateTime.UtcNow - user.LastPasswordChanged.Value < TimeSpan.FromMinutes(30))
            {
                ModelState.AddModelError(string.Empty, "You cannot change your password within 30 minutes of the last change.");
                return Page();
            }

            // Check password history
            var passwordHistories = dbContext.PasswordHistories
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.CreatedAt)
                .Take(2)
                .ToList();

            foreach (var history in passwordHistories)
            {
                if (userManager.PasswordHasher.VerifyHashedPassword(user, history.HashedPassword, Input.NewPassword) != PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError(string.Empty, "You cannot reuse your previous passwords.");
                    return Page();
                }
            }

            // Change password
            var result = await userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // Save new password to history
            dbContext.PasswordHistories.Add(new PasswordHistory
            {
                UserId = user.Id,
                HashedPassword = userManager.PasswordHasher.HashPassword(user, Input.NewPassword),
                CreatedAt = DateTime.UtcNow
            });

            // Update last password change timestamp
            user.LastPasswordChanged = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
            await dbContext.SaveChangesAsync();

            await signInManager.RefreshSignInAsync(user);
            return RedirectToPage("Index");
        }

        public class ChangePasswordInput
        {
            [Required]
            [DataType(DataType.Password)]
            public string OldPassword { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters long.")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$",
                ErrorMessage = "Password must include uppercase, lowercase, number, and special character.")]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }
    }
}