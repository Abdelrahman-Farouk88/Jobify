using Jobify.Web.Data;
using Jobify.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobify.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var jobSeekerRoleId = await dbContext.Roles
            .Where(role => role.Name == "JobSeeker")
            .Select(role => role.Id)
            .SingleAsync();

        var employerRoleId = await dbContext.Roles
            .Where(role => role.Name == "Employer")
            .Select(role => role.Id)
            .SingleAsync();

        var allUsers = await dbContext.Users.ToListAsync();
        var allUserRoles = await dbContext.UserRoles.ToListAsync();
        var allRoles = await dbContext.Roles.ToListAsync();

        var userViewModels = allUsers
            .Where(u => !allUserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == allRoles.FirstOrDefault(r => r.Name == "Admin")?.Id))
            .Select(u => new ApplicationUserViewModel
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                Role = allRoles.FirstOrDefault(r => r.Id == allUserRoles.FirstOrDefault(ur => ur.UserId == u.Id)?.RoleId)?.Name ?? "None",
                DisplayName = u.UserName ?? string.Empty
            }).ToList();

        var pendingJobs = await dbContext.JobPostings
            .Where(job => job.Status == JobStatus.Pending)
            .OrderByDescending(job => job.PostedAtUtc)
            .ToListAsync();

        var pendingEmployerProfiles = await dbContext.EmployerProfiles
            .Where(ep => !ep.IsApproved && !ep.IsRejected)
            .OrderByDescending(ep => ep.CreatedAtUtc)
            .ToListAsync();

        var pendingEmployerUserIds = pendingEmployerProfiles.Select(ep => ep.UserId).ToList();
        var pendingEmployerUsers = await dbContext.Users
            .Where(u => pendingEmployerUserIds.Contains(u.Id))
            .ToListAsync();

        var pendingEmployers = pendingEmployerProfiles.Select(ep => new EmployerApprovalViewModel
        {
            ProfileId = ep.Id,
            UserId = ep.UserId,
            Email = pendingEmployerUsers.FirstOrDefault(u => u.Id == ep.UserId)?.Email ?? "",
            CompanyName = ep.CompanyName,
            Website = ep.Website,
            Description = ep.Description,
            RegisteredAt = ep.CreatedAtUtc
        }).ToList();

        var viewModel = new AdminDashboardViewModel
        {
            JobSeekerCount = await dbContext.UserRoles.CountAsync(role => role.RoleId == jobSeekerRoleId),
            EmployerCount = await dbContext.UserRoles.CountAsync(role => role.RoleId == employerRoleId),
            JobCount = await dbContext.JobPostings.CountAsync(),
            ApplicationCount = await dbContext.JobApplications.CountAsync(),
            NotificationCount = await dbContext.RecruitmentNotifications.CountAsync(),
            ActiveJobs = await dbContext.JobPostings.CountAsync(job => job.IsActive),
            InactiveJobs = await dbContext.JobPostings.CountAsync(job => !job.IsActive),
            SubmittedApplications = await dbContext.JobApplications.CountAsync(application => application.Status == ApplicationStatus.UnderReview),
            ReviewedApplications = await dbContext.JobApplications.CountAsync(application => application.Status != ApplicationStatus.UnderReview),
            PendingJobs = pendingJobs,
            PendingEmployers = pendingEmployers,
            Users = userViewModels
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Reports()
    {
        var jobSeekerRoleId = await dbContext.Roles.Where(role => role.Name == "JobSeeker").Select(role => role.Id).SingleAsync();
        var employerRoleId = await dbContext.Roles.Where(role => role.Name == "Employer").Select(role => role.Id).SingleAsync();

        var applicationStatusCounts = Enum.GetValues<ApplicationStatus>()
            .Select(status => new StatusCountViewModel
            {
                Label = status.ToString(),
                Count = dbContext.JobApplications.Count(application => application.Status == status)
            })
            .ToList();

        var jobCategoryCounts = await dbContext.JobPostings
            .AsNoTracking()
            .GroupBy(job => job.Category)
            .Select(group => new CategoryCountViewModel
            {
                Label = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ToListAsync();

        return View(new AdminReportsViewModel
        {
            TotalUsers = await dbContext.Users.CountAsync(),
            JobSeekers = await dbContext.UserRoles.CountAsync(role => role.RoleId == jobSeekerRoleId),
            Employers = await dbContext.UserRoles.CountAsync(role => role.RoleId == employerRoleId),
            Jobs = await dbContext.JobPostings.CountAsync(),
            Applications = await dbContext.JobApplications.CountAsync(),
            ApplicationStatusCounts = applicationStatusCounts,
            JobCategoryCounts = jobCategoryCounts
        });
    }

    public async Task<IActionResult> ManageJobs()
    {
        var jobs = await dbContext.JobPostings
            .OrderByDescending(j => j.PostedAtUtc)
            .ToListAsync();
        return View(jobs);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteJob(int id)
    {
        var job = await dbContext.JobPostings.FindAsync(id);
        if (job != null)
        {
            dbContext.JobPostings.Remove(job);
            await dbContext.SaveChangesAsync();
        }
        return RedirectToAction(nameof(ManageJobs));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveJob(int id)
    {
        var job = await dbContext.JobPostings.FindAsync(id);
        if (job != null)
        {
            job.Status = JobStatus.Approved;
            await dbContext.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectJob(int id, string reason)
    {
        var job = await dbContext.JobPostings.FindAsync(id);
        if (job != null)
        {
            job.Status = JobStatus.Rejected;
            job.RejectionReason = reason;
            await dbContext.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveEmployer(int profileId)
    {
        var profile = await dbContext.EmployerProfiles.FindAsync(profileId);
        if (profile != null)
        {
            profile.IsApproved = true;
            await dbContext.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectEmployer(int profileId)
    {
        var profile = await dbContext.EmployerProfiles.FindAsync(profileId);
        if (profile != null)
        {
            profile.IsRejected = true;
            profile.IsApproved = false;
            await dbContext.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ViewEmployer(int profileId)
    {
        var profile = await dbContext.EmployerProfiles.FindAsync(profileId);
        if (profile is null) return NotFound();

        var user = await dbContext.Users.FindAsync(profile.UserId);
        ViewBag.Email = user?.Email ?? "Unknown";
        ViewBag.Name = user?.UserName ?? "Unknown";
        return View(profile);
    }
}