using Jobify.Web.Data;
using Jobify.Web.Models;
using Jobify.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobify.Web.Controllers;

public class JobsController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, JobMatchService jobMatchService) : Controller
{
    public async Task<IActionResult> Index(string? search, string? category, string? location, string? skill)
    {
        var jobsQuery = dbContext.JobPostings
            .AsNoTracking()
            .Where(job => job.IsActive && job.Status == JobStatus.Approved);

        if (!string.IsNullOrWhiteSpace(search))
        {
            jobsQuery = jobsQuery.Where(job => job.Title.Contains(search) || job.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            jobsQuery = jobsQuery.Where(job => job.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            jobsQuery = jobsQuery.Where(job => job.Location.Contains(location));
        }

        if (!string.IsNullOrWhiteSpace(skill))
        {
            jobsQuery = jobsQuery.Where(job => job.RequiredSkills.Contains(skill));
        }

        var jobs = await jobsQuery
            .OrderByDescending(job => job.PostedAtUtc)
            .ToListAsync();

        CandidateProfile? candidateProfile = null;
        var userId = userManager.GetUserId(User);
        if (User.IsInRole("JobSeeker") && !string.IsNullOrWhiteSpace(userId))
        {
            candidateProfile = await dbContext.CandidateProfiles.AsNoTracking().FirstOrDefaultAsync(profile => profile.UserId == userId);
        }

        var jobResults = jobs
            .Select(job =>
            {
                var match = jobMatchService.Score(candidateProfile, job);
                return new JobSearchResultViewModel
                {
                    Job = job,
                    MatchScore = match.MatchScore,
                    MatchReasons = match.MatchReasons,
                    MatchSource = match.MatchSource
                };
            })
            .OrderByDescending(result => result.MatchScore ?? 0)
            .ThenByDescending(result => result.Job.PostedAtUtc)
            .ToList();

        var categories = await dbContext.JobPostings
            .AsNoTracking()
            .Select(job => job.Category)
            .Distinct()
            .OrderBy(categoryName => categoryName)
            .ToListAsync();

        return View(new JobSearchViewModel
        {
            Search = search,
            Category = category,
            Location = location,
            Skill = skill,
            Jobs = jobResults,
            Categories = categories
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var job = await dbContext.JobPostings.AsNoTracking().FirstOrDefaultAsync(jobPosting => jobPosting.Id == id && (jobPosting.Status == JobStatus.Approved || jobPosting.EmployerId == userManager.GetUserId(User) || User.IsInRole("Admin")));

        if (job is null)
        {
            return NotFound();
        }

        var hasApplied = User.Identity?.IsAuthenticated == true && await dbContext.JobApplications
            .AsNoTracking()
            .AnyAsync(application => application.JobPostingId == id && application.ApplicantId == userManager.GetUserId(User));

        var userId = userManager.GetUserId(User);
        var candidateProfile = User.IsInRole("JobSeeker") && !string.IsNullOrWhiteSpace(userId)
            ? await dbContext.CandidateProfiles.AsNoTracking().FirstOrDefaultAsync(profile => profile.UserId == userId)
            : null;

        var match = jobMatchService.Score(candidateProfile, job);

        return View(new JobDetailsViewModel
        {
            Job = job,
            HasApplied = hasApplied,
            MatchScore = match.MatchScore,
            MatchReasons = match.MatchReasons,
            MatchSource = match.MatchSource
        });
    }

    [Authorize(Roles = "JobSeeker")]
    public async Task<IActionResult> Apply(int id)
    {
        var job = await dbContext.JobPostings.AsNoTracking().FirstOrDefaultAsync(jobPosting => jobPosting.Id == id);

        if (job is null)
        {
            return NotFound();
        }

        var userId = userManager.GetUserId(User) ?? string.Empty;
        var candidateProfile = await dbContext.CandidateProfiles.AsNoTracking().FirstOrDefaultAsync(profile => profile.UserId == userId);

        if (candidateProfile is null || string.IsNullOrWhiteSpace(candidateProfile.ResumeUrl))
        {
            TempData["ProfileMessage"] = "Upload your CV in your profile before applying.";
            return RedirectToAction("Profile", "Candidate");
        }

        var match = jobMatchService.Score(candidateProfile, job);
        if ((match.MatchScore ?? 0) < 50)
        {
            TempData["ProfileMessage"] = "You cannot apply for this job because your profile match is below 50%. Please update your profile to match the requirements better.";
            return RedirectToAction(nameof(Details), new { id = job.Id });
        }

        ViewBag.Job = job;
        return View(new JobApplicationInputModel { JobPostingId = job.Id, ResumeUrl = candidateProfile.ResumeUrl });
    }

    [HttpPost]
    [Authorize(Roles = "JobSeeker")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(JobApplicationInputModel inputModel)
    {
        var job = await dbContext.JobPostings.FirstOrDefaultAsync(jobPosting => jobPosting.Id == inputModel.JobPostingId && jobPosting.IsActive);

        if (job is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(inputModel);
        }

        var applicantId = userManager.GetUserId(User) ?? string.Empty;
        var applicantName = User.Identity?.Name ?? "Job seeker";
        var candidateProfile = await dbContext.CandidateProfiles.AsNoTracking().FirstOrDefaultAsync(profile => profile.UserId == applicantId);

        if (candidateProfile is null || string.IsNullOrWhiteSpace(candidateProfile.ResumeUrl))
        {
            TempData["ProfileMessage"] = "Upload your CV in your profile before applying.";
            return RedirectToAction("Profile", "Candidate");
        }

        var match = jobMatchService.Score(candidateProfile, job);
        if ((match.MatchScore ?? 0) < 50)
        {
            TempData["ProfileMessage"] = "You cannot apply for this job because your profile match is below 50%.";
            return RedirectToAction(nameof(Details), new { id = job.Id });
        }

        var alreadyApplied = await dbContext.JobApplications
            .AnyAsync(application => application.JobPostingId == inputModel.JobPostingId && application.ApplicantId == applicantId);

        if (alreadyApplied)
        {
            ModelState.AddModelError(string.Empty, "You have already applied for this job.");
            return View(inputModel);
        }

        var application = new JobApplication
        {
            JobPostingId = inputModel.JobPostingId,
            ApplicantId = applicantId,
            ApplicantName = applicantName,
            ResumeUrl = candidateProfile.ResumeUrl,
            CoverLetter = inputModel.CoverLetter,
            AssessmentAnswer = inputModel.AssessmentAnswer,
            AppliedAtUtc = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(job.AssessmentPrompt))
        {
            if (string.IsNullOrWhiteSpace(inputModel.AssessmentAnswer))
            {
                application.Status = ApplicationStatus.Rejected;
                application.ReviewerNotes = "Auto-rejected: Missing assessment answer.";
                application.ReviewedAtUtc = DateTime.UtcNow;
            }
            else if (!string.IsNullOrWhiteSpace(job.AssessmentExpectedAnswer) && 
                     !inputModel.AssessmentAnswer.Contains(job.AssessmentExpectedAnswer, StringComparison.OrdinalIgnoreCase))
            {
                application.Status = ApplicationStatus.Rejected;
                application.ReviewerNotes = "Auto-rejected: Failed assessment.";
                application.ReviewedAtUtc = DateTime.UtcNow;
            }
            // Passed assessment — stays UnderReview for employer to decide
        }

        dbContext.JobApplications.Add(application);

        if (!string.IsNullOrWhiteSpace(job.EmployerId))
        {
            dbContext.RecruitmentNotifications.Add(new RecruitmentNotification
            {
                UserId = job.EmployerId,
                Subject = $"New application for {job.Title}",
                Body = $"{applicantName} submitted an application for {job.Title}.",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = inputModel.JobPostingId });
    }

}