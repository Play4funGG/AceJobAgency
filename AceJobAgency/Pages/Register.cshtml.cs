using AceJobAgency.Model;
using AceJobAgency.Utilities;
using AceJobAgency.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;

namespace AceJobAgency.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;

        [BindProperty]
        public Register RModel { get; set; }

        public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid input. Please check your entries.";
                return Page();
            }

            // Validate email format
            if (!IsValidEmail(RModel.Email))
            {
                ModelState.AddModelError("", "Invalid email format.");
                return Page();
            }

            // Check for duplicate email in the database
            var existingUser = await userManager.FindByEmailAsync(RModel.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("RModel.Email", "Email is already in use.");
                return Page();
            }

            // Validate date of birth
            if (RModel.DateOfBirth > DateTime.UtcNow.AddYears(-18))
            {
                ModelState.AddModelError("", "You must be at least 18 years old.");
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = RModel.Email,
                Email = RModel.Email,
                FirstName = System.Net.WebUtility.HtmlEncode(RModel.FirstName),
                LastName = System.Net.WebUtility.HtmlEncode(RModel.LastName),
                Gender = RModel.Gender,
                EncryptedNRIC = EncryptionHelper.Encrypt(RModel.NRIC),
                DateOfBirth = RModel.DateOfBirth,
                WhoAmI = System.Net.WebUtility.HtmlEncode(RModel.WhoAmI),
                ResumePath = string.Empty,
                LastPasswordChanged = DateTime.UtcNow // Set the timestamp
            };

            var result = await userManager.CreateAsync(user, RModel.Password);
            if (result.Succeeded)
            {
                if (RModel.Resume != null && RModel.Resume.Length > 0)
                {
                    var allowedExtensions = new[] { ".docx", ".pdf" };
                    var fileExtension = Path.GetExtension(RModel.Resume.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("", "Only .docx and .pdf files are allowed.");
                        return Page();
                    }

                    var filePath = Path.Combine("wwwroot", "uploads", RModel.Resume.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await RModel.Resume.CopyToAsync(stream);
                    }

                    user.ResumePath = filePath;
                    await userManager.UpdateAsync(user);
                }

                await signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToPage("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

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
    }
}