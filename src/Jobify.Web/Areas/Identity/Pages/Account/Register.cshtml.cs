using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Jobify.Web.Areas.Identity.Pages.Account;

public class RegisterModel(UserManager<IdentityUser> userManager, IUserStore<IdentityUser> userStore, SignInManager<IdentityUser> signInManager, ILogger<RegisterModel> logger, Jobify.Web.Data.ApplicationDbContext dbContext) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password), Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password), Display(Name = "Confirm password"), Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required, Display(Name = "Account type")]
        public string Role { get; set; } = "JobSeeker";

        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Display(Name = "Company Website")]
        public string? CompanyWebsite { get; set; }

        [Display(Name = "Company Description")]
        public string? CompanyDescription { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Input.Role is not ("JobSeeker" or "Employer"))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Role)}", "Choose a valid account type.");
            return Page();
        }

        if (Input.Role == "Employer" && string.IsNullOrWhiteSpace(Input.CompanyName))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.CompanyName)}", "Company Name is required for Employers.");
            return Page();
        }

        var user = CreateUser();
        await userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
        await ((IUserEmailStore<IdentityUser>)userStore).SetEmailAsync(user, Input.Email, CancellationToken.None);

        var result = await userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        await userManager.AddToRoleAsync(user, Input.Role);

        if (Input.Role == "Employer")
        {
            dbContext.EmployerProfiles.Add(new Jobify.Web.Models.EmployerProfile
            {
                UserId = user.Id,
                CompanyName = Input.CompanyName ?? string.Empty,
                Website = Input.CompanyWebsite ?? string.Empty,
                Description = Input.CompanyDescription ?? string.Empty,
                Industry = "Not Specified",
                Location = "Not Specified",
                CreatedAtUtc = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        logger.LogInformation("User created a new account with password.");

        await signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(returnUrl);
    }

    private static IdentityUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<IdentityUser>();
        }
        catch
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'.");
        }
    }
}
