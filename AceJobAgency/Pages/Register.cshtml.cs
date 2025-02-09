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

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = RModel.Email,
                    Email = RModel.Email,
                    FirstName = RModel.FirstName,
                    LastName = RModel.LastName,
                    Gender = RModel.Gender,
                    EncryptedNRIC = EncryptionHelper.Encrypt(RModel.NRIC),
                    DateOfBirth = RModel.DateOfBirth,
                    WhoAmI = RModel.WhoAmI,
                    ResumePath = string.Empty // Default value if no resume is uploaded
                };

                var result = await userManager.CreateAsync(user, RModel.Password);

                if (result.Succeeded)
                {
                    // Handle file upload
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
            }

            return Page();
        }
    }
}