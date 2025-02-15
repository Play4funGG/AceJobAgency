using AceJobAgency.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

public class OTPVerificationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AuthDbContext _dbContext;

    public OTPVerificationModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, AuthDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
    }

    [BindProperty]
    public string OTPCode { get; set; }

    public string Email { get; private set; }
    public string UserId { get; private set; }

    public async Task OnGetAsync()
    {
        // Retrieve Email and UserId from the session
        UserId = HttpContext.Session.GetString("UserId");

        if (string.IsNullOrEmpty(UserId))
        {
            TempData["ErrorMessage"] = "User ID not found.";
            RedirectToPage("Login");
            return;
        }

        var user = await _userManager.FindByIdAsync(UserId);
        if (user != null)
        {
            Email = user.Email;
        }
        else
        {
            TempData["ErrorMessage"] = "User not found.";
            RedirectToPage("Login");
        }
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(OTPCode))
        {
            TempData["ErrorMessage"] = "OTP cannot be empty.";
            return Page();
        }

        // Retrieve UserId from sessionx
        UserId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(UserId))
        {
            TempData["ErrorMessage"] = "User ID not found.";
            return RedirectToPage("Login");
        }

        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage("Login");
        }

        // Retrieve OTP from TempData
        var storedOTP = TempData["OTPCode"]?.ToString(); // Retrieve OTP from TempData

        if (string.IsNullOrEmpty(storedOTP) || OTPCode != storedOTP)
        {
            TempData["ErrorMessage"] = "Invalid OTP.";
            return Page();
        }

        // OTP is correct, clear TempData for OTP
        TempData.Remove("OTPCode");

        // Sign the user in after OTP verification
        await _signInManager.SignInAsync(user, isPersistent: false);

        // Redirect to Index page after successful login
        return RedirectToPage("Index");
    }
}
