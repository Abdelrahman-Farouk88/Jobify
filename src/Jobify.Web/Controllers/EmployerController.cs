using Jobify.Web.Data;
using Jobify.Web.Models;
using Jobify.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobify.Web.Controllers;

[Authorize(Roles = "Employer")]
public class EmployerController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, JobMatchService jobMatchService) : Controller
{
    public async Task<IActionResult> Dashboard()
    {
        var employerId = userManager.GetUserId(User) ?? string.Empty;
        var employerProfile = await dbContext.EmployerProfiles.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == employerId);
        
        if (employerProfile != null && employerProfile.IsRejected)
        {
            return RedirectToAction(nameof(RejectedAccount));
        }

        if (employerProfile != null && !employerProfile.IsApproved)
        {
            return RedirectToAction(nameof(PendingApproval));
        }

        var jobs = await dbContext.JobPostings
            .AsNoTracking()
            .Where(job => job.EmployerId == employerId)
            .OrderByDescending(job => job.PostedAtUtc)
            .ToListAsync();

        var applications = await dbContext.JobApplications
            .AsNoTracking()
            .Where(application => dbContext.JobPostings.Any(job => job.Id == application.JobPostingId && job.EmployerId == employerId))
            .OrderByDescending(application => application.AppliedAtUtc)
            .ToListAsync();

        return View(new EmployerDashboardViewModel
        {
            Jobs = jobs,
            Applications = applications,
            OpenJobs = jobs.Count(job => job.Status == JobStatus.Approved && job.IsActive),
            TotalApplications = applications.Count,
            ReviewedApplications = applications.Count(application => application.Status != ApplicationStatus.UnderReview),
            SubmittedApplications = applications.Count(application => application.Status == ApplicationStatus.UnderReview)
        });
    }

    public async Task<IActionResult> Create()
    {
        var employerId = userManager.GetUserId(User) ?? string.Empty;
        var employerProfile = await dbContext.EmployerProfiles.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == employerId);
        
        if (employerProfile != null && employerProfile.IsRejected)
            return RedirectToAction(nameof(RejectedAccount));

        if (employerProfile != null && !employerProfile.IsApproved)
            return RedirectToAction(nameof(PendingApproval));

        return View(new JobPostingInputModel());
    }

    public async Task<IActionResult> EditJob(int id)
    {
        var employerId = userManager.GetUserId(User) ?? string.Empty;
        var job = await dbContext.JobPostings.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id && j.EmployerId == employerId);
        if (job == null) return NotFound();
        if (job.Status != JobStatus.Rejected) return RedirectToAction(nameof(Dashboard));

        var model = new JobPostingInputModel
        {
            Title = job.Title,
            Category = job.Category,
            Location = job.Location,
            RequiredSkills = job.RequiredSkills,
            Description = job.Description,
            EmploymentType = job.EmploymentType,
            ExperienceLevel = job.ExperienceLevel,
            AssessmentPrompt = job.AssessmentPrompt,
            AssessmentExpectedAnswer = job.AssessmentExpectedAnswer
        };
        ViewBag.JobId = id;
        ViewBag.RejectionReason = job.RejectionReason;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditJob(int id, JobPostingInputModel inputModel)
    {
        var employerId = userManager.GetUserId(User) ?? string.Empty;
        var job = await dbContext.JobPostings.FirstOrDefaultAsync(j => j.Id == id && j.EmployerId == employerId);
        if (job == null) return NotFound();
        if (job.Status != JobStatus.Rejected)
        {
            ModelState.AddModelError("", "Only rejected jobs can be edited and resubmitted.");
            ViewBag.JobId = id;
            return View(inputModel);
        }

        if (!ModelState.IsValid)
        {
            ViewBag.JobId = id;
            return View(inputModel);
        }

        job.Title = inputModel.Title;
        job.Category = inputModel.Category;
        job.Location = inputModel.Location;
        job.RequiredSkills = inputModel.RequiredSkills;
        job.Description = inputModel.Description;
        job.EmploymentType = inputModel.EmploymentType;
        job.ExperienceLevel = inputModel.ExperienceLevel;
        job.AssessmentPrompt = inputModel.AssessmentPrompt;
        job.AssessmentExpectedAnswer = inputModel.AssessmentExpectedAnswer;
        job.Status = JobStatus.Pending;
        job.PostedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JobPostingInputModel inputModel)
    {
        if (!ModelState.IsValid)
        {
            return View(inputModel);
        }

        var employerId = userManager.GetUserId(User) ?? string.Empty;
        var employerProfile = await dbContext.EmployerProfiles.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == employerId);
        
        if (employerProfile != null && !employerProfile.IsApproved)
        {
            return RedirectToAction(nameof(PendingApproval));
        }

        var job = new JobPosting
        {
            Title = inputModel.Title,
            Category = inputModel.Category,
            Location = inputModel.Location,
            RequiredSkills = inputModel.RequiredSkills,
            Description = inputModel.Description,
            EmploymentType = inputModel.EmploymentType,
            ExperienceLevel = inputModel.ExperienceLevel,
            AssessmentPrompt = inputModel.AssessmentPrompt,
            AssessmentExpectedAnswer = inputModel.AssessmentExpectedAnswer,
            EmployerName = User.Identity?.Name ?? "Employer",
            EmployerId = employerId,
            PostedAtUtc = DateTime.UtcNow,
            IsActive = true,
            Status = JobStatus.Pending
        };

        dbContext.JobPostings.Add(job);

        await dbContext.SaveChangesAsync();

        var candidateProfiles = await dbContext.CandidateProfiles.AsNoTracking().ToListAsync();
        foreach (var profile in candidateProfiles)
        {
            var match = jobMatchService.Score(profile, job);
            if ((match.MatchScore ?? 0) >= 50)
            {
                dbContext.RecruitmentNotifications.Add(new RecruitmentNotification
                {
                    UserId = profile.UserId,
                    Subject = "New High-Match Job Mission",
                    Body = $"A new job mission '{job.Title}' matches your profile with a score of {match.MatchScore}%. Check it out and initiate an application!",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Dashboard));
    }

    public async Task<IActionResult> Applications(int jobId, bool filterShortlisted = false)
    {
        var employerId = userManager.GetUserId(User) ?? string.Empty;
        var employerProfile = await dbContext.EmployerProfiles.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == employerId);
        
        if (employerProfile != null && employerProfile.IsRejected)
            return RedirectToAction(nameof(RejectedAccount));

        if (employerProfile != null && !employerProfile.IsApproved)
            return RedirectToAction(nameof(PendingApproval));

        var ownsJob = await dbContext.JobPostings.AnyAsync(job => job.Id == jobId && job.EmployerId == employerId);
        if (!ownsJob)
        {
            return NotFound();
        }

        var job = await dbContext.JobPostings.AsNoTracking().FirstAsync(j => j.Id == jobId);
        ViewBag.Job = job;
        ViewBag.FilterShortlisted = filterShortlisted;

        var query = dbContext.JobApplications
            .AsNoTracking()
            .Where(application => application.JobPostingId == jobId);
            
        if (filterShortlisted)
        {
            query = query.Where(a => a.IsShortlisted);
        }

        var applications = await query
            .OrderByDescending(application => application.AppliedAtUtc)
            .ToListAsync();

        var candidateIds = applications.Select(a => a.ApplicantId).Distinct().ToList();
        var candidates = await dbContext.CandidateProfiles
            .AsNoTracking()
            .Where(c => candidateIds.Contains(c.UserId))
            .ToDictionaryAsync(c => c.UserId);

        var matchScores = new Dictionary<int, decimal>();
        foreach (var application in applications)
        {
            if (candidates.TryGetValue(application.ApplicantId, out var candidate))
            {
                var match = jobMatchService.Score(candidate, job);
                matchScores[application.Id] = match.MatchScore ?? 0m;
            }
            else
            {
                matchScores[application.Id] = 0m;
            }
        }
        ViewBag.MatchScores = matchScores;

        return View(applications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewApplication(int applicationId, string status, string? notes)
    {
        var employerId = userManager.GetUserId(User) ?? string.Empty;

        var application = await dbContext.JobApplications
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId && dbContext.JobPostings.Any(job => job.Id == candidate.JobPostingId && job.EmployerId == employerId));

        if (application is null)
        {
            return NotFound();
        }

        if (Enum.TryParse<ApplicationStatus>(status, true, out var parsedStatus))
        {
            application.Status = parsedStatus;
        }

        application.ReviewerNotes = notes;
        application.ReviewedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Applications), new { jobId = application.JobPostingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleShortlist(int applicationId, bool currentFilterState)
    {
        var employerId = userManager.GetUserId(User) ?? string.Empty;

        var application = await dbContext.JobApplications
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId && dbContext.JobPostings.Any(job => job.Id == candidate.JobPostingId && job.EmployerId == employerId));

        if (application is null)
        {
            return NotFound();
        }

        application.IsShortlisted = !application.IsShortlisted;
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Applications), new { jobId = application.JobPostingId, filterShortlisted = currentFilterState });
    }

    public async Task<IActionResult> Reports()
    {
        var employerId = userManager.GetUserId(User) ?? string.Empty;
        var employerProfile = await dbContext.EmployerProfiles.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == employerId);
        
        if (employerProfile != null && employerProfile.IsRejected)
            return RedirectToAction(nameof(RejectedAccount));

        if (employerProfile != null && !employerProfile.IsApproved)
            return RedirectToAction(nameof(PendingApproval));

        var jobs = await dbContext.JobPostings.AsNoTracking().Where(job => job.EmployerId == employerId).ToListAsync();
        var applications = await dbContext.JobApplications
            .AsNoTracking()
            .Where(application => dbContext.JobPostings.Any(job => job.Id == application.JobPostingId && job.EmployerId == employerId))
            .ToListAsync();

        var applicationStatusCounts = Enum.GetValues<ApplicationStatus>()
            .Select(status => new StatusCountViewModel
            {
                Label = status.ToString(),
                Count = applications.Count(application => application.Status == status)
            })
            .ToList();

        var approvedJobs = jobs.Where(job => job.Status == JobStatus.Approved).ToList();

        var categoryCounts = approvedJobs
            .GroupBy(job => job.Category)
            .Select(group => new CategoryCountViewModel
            {
                Label = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ToList();

        return View(new EmployerReportsViewModel
        {
            EmployerName = User.Identity?.Name ?? "Employer",
            JobsPosted = approvedJobs.Count,
            ActiveJobs = approvedJobs.Count(job => job.IsActive),
            TotalApplications = applications.Count,
            ReviewBacklog = applications.Count(application => application.Status == ApplicationStatus.UnderReview),
            ApplicationStatusCounts = applicationStatusCounts,
            JobCategoryCounts = categoryCounts
        });
    }

    [AllowAnonymous]
    public IActionResult PendingApproval()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult RejectedAccount()
    {
        return View();
    }
}