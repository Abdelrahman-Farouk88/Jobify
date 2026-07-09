using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Jobify.Web.Data;

namespace Jobify.Web.Data;

public static class IdentitySeeder
{
    private static readonly string[] Roles = ["Admin", "Employer", "JobSeeker"];

    private static readonly (string Email, string Password, string Role)[] DemoUsers =
    [
        ("admin@jobify.local", "Admin123!", "Admin"),
        ("employer@jobify.local", "Employer123!", "Employer"),
        ("seeker@jobify.local", "Seeker123!", "JobSeeker")
    ];

    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var roleName in Roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        foreach (var (email, password, role) in DemoUsers)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is not null)
            {
                continue;
            }

            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var employer = await userManager.FindByEmailAsync("employer@jobify.local");
        if (employer is not null)
        {
            var jobsWithoutEmployer = await dbContext.JobPostings
                .Where(job => job.EmployerId == null || job.EmployerId == string.Empty)
                .ToListAsync();

            foreach (var job in jobsWithoutEmployer)
            {
                job.EmployerId = employer.Id;
            }

            if (jobsWithoutEmployer.Count > 0)
            {
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
