using Jobify.Web.Data;
using Jobify.Web.Models;
using Jobify.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobify.Web.Controllers;

[Authorize(Roles = "JobSeeker")]
public class CandidateController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, ResumeStorageService resumeStorageService, JobMatchService jobMatchService) : Controller
{
    public async Task<IActionResult> Profile()
    {
        var userId = userManager.GetUserId(User) ?? string.Empty;
        var profile = await dbContext.CandidateProfiles.AsNoTracking().FirstOrDefaultAsync(candidateProfile => candidateProfile.UserId == userId);

        if (profile is not null)
        {
            var activeJobs = await dbContext.JobPostings.AsNoTracking().Where(job => job.IsActive).ToListAsync();
            var matches = new List<JobSearchResultViewModel>();
            foreach (var job in activeJobs)
            {
                var match = jobMatchService.Score(profile, job);
                matches.Add(new JobSearchResultViewModel
                {
                    Job = job,
                    MatchScore = match.MatchScore,
                    MatchReasons = match.MatchReasons,
                    MatchSource = match.MatchSource
                });
            }
            ViewBag.TopMatches = matches.Where(m => m.MatchScore > 0).OrderByDescending(m => m.MatchScore).Take(5).ToList();
        }

        return View(profile is null
            ? new CandidateProfileInputModel()
            : new CandidateProfileInputModel
            {
                FullName = profile.FullName,
                Headline = profile.Headline,
                Location = profile.Location,
                Skills = profile.Skills,
                ExperienceSummary = profile.ExperienceSummary,
                CurrentResumeFileName = profile.ResumeFileName,
                CurrentResumeUrl = profile.ResumeUrl,
                PortfolioLink1 = profile.PortfolioLink1,
                PortfolioLink2 = profile.PortfolioLink2
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(CandidateProfileInputModel inputModel)
    {
        if (!ModelState.IsValid)
        {
            return View(inputModel);
        }

        var userId = userManager.GetUserId(User) ?? string.Empty;
        var existingProfile = await dbContext.CandidateProfiles.FirstOrDefaultAsync(candidateProfile => candidateProfile.UserId == userId);

        if (inputModel.ResumeFile is null && existingProfile?.ResumeUrl is null)
        {
            ModelState.AddModelError(nameof(inputModel.ResumeFile), "Upload a CV in PDF, DOCX, or TXT format.");
            return View(inputModel);
        }

        StoredResume? storedResume = null;

        if (inputModel.ResumeFile is not null)
        {
            storedResume = await resumeStorageService.SaveAsync(inputModel.ResumeFile, userId, existingProfile?.ResumeStoredPath);
        }

        if (existingProfile is null)
        {
            dbContext.CandidateProfiles.Add(new CandidateProfile
            {
                UserId = userId,
                FullName = inputModel.FullName,
                Headline = inputModel.Headline,
                Location = inputModel.Location,
                Skills = inputModel.Skills,
                ExperienceSummary = inputModel.ExperienceSummary,
                ResumeFileName = storedResume?.FileName,
                ResumeStoredPath = storedResume?.StoredPath,
                ResumeContentType = storedResume?.ContentType,
                ResumeText = storedResume?.ExtractedText,
                ResumeUrl = storedResume?.RelativeUrl,
                ResumeUploadedAtUtc = storedResume is null ? null : DateTime.UtcNow,
                PortfolioLink1 = inputModel.PortfolioLink1,
                PortfolioLink2 = inputModel.PortfolioLink2,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existingProfile.FullName = inputModel.FullName;
            existingProfile.Headline = inputModel.Headline;
            existingProfile.Location = inputModel.Location;
            existingProfile.Skills = inputModel.Skills;
            existingProfile.ExperienceSummary = inputModel.ExperienceSummary;
            existingProfile.PortfolioLink1 = inputModel.PortfolioLink1;
            existingProfile.PortfolioLink2 = inputModel.PortfolioLink2;
            if (storedResume is not null)
            {
                existingProfile.ResumeFileName = storedResume.FileName;
                existingProfile.ResumeStoredPath = storedResume.StoredPath;
                existingProfile.ResumeContentType = storedResume.ContentType;
                existingProfile.ResumeText = storedResume.ExtractedText;
                existingProfile.ResumeUrl = storedResume.RelativeUrl;
                existingProfile.ResumeUploadedAtUtc = DateTime.UtcNow;
            }
            existingProfile.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        TempData["ProfileMessage"] = "Coordinates updated. Re-calculating matches.";
        return RedirectToAction(nameof(Profile));
    }

    public async Task<IActionResult> Applications()
    {
        var userId = userManager.GetUserId(User) ?? string.Empty;
        var applications = await dbContext.JobApplications
            .Include(a => a.JobPosting)
            .Where(a => a.ApplicantId == userId)
            .OrderByDescending(a => a.AppliedAtUtc)
            .AsNoTracking()
            .ToListAsync();

        return View(applications);
    }
}