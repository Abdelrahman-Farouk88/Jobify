using Microsoft.EntityFrameworkCore;
using Jobify.Web.Models;

namespace Jobify.Web.Data;

public static class PlatformSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!await dbContext.JobPostings.AnyAsync())
        {
            dbContext.JobPostings.AddRange(
                new JobPosting
                {
                    Title = "Junior .NET Developer",
                    Category = "Software Development",
                    Location = "Remote",
                    RequiredSkills = "C#, ASP.NET Core, SQL, Git",
                    Description = "Build internal tools and candidate-facing features for a growing hiring platform.",
                    EmploymentType = "Full-time",
                    ExperienceLevel = "Entry-level",
                    EmployerName = "Jobify Labs",
                    PostedAtUtc = DateTime.UtcNow.AddDays(-2),
                    IsActive = true
                },
                new JobPosting
                {
                    Title = "UI/UX Intern",
                    Category = "Design",
                    Location = "Hybrid",
                    RequiredSkills = "Figma, Wireframing, Responsive Design",
                    Description = "Design a simple and polished candidate journey for the recruitment platform.",
                    EmploymentType = "Internship",
                    ExperienceLevel = "Student",
                    EmployerName = "Campus Careers",
                    PostedAtUtc = DateTime.UtcNow.AddDays(-1),
                    IsActive = true
                },
                new JobPosting
                {
                    Title = "Recruitment Operations Associate",
                    Category = "Operations",
                    Location = "On-site",
                    RequiredSkills = "Communication, Screening, Coordination",
                    Description = "Support application review, employer communication, and candidate notifications.",
                    EmploymentType = "Full-time",
                    ExperienceLevel = "0-2 years",
                    EmployerName = "Talent Bridge",
                    PostedAtUtc = DateTime.UtcNow.AddDays(-4),
                    IsActive = true
                });

            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.CandidateProfiles.AnyAsync() && !await dbContext.JobApplications.AnyAsync())
        {
            var jobs = await dbContext.JobPostings.OrderBy(job => job.Id).ToListAsync();

            var candidateProfiles = new[]
            {
                new CandidateProfile
                {
                    UserId = "seed-jobseeker-1",
                    FullName = "Amina Yusuf",
                    Headline = "Junior .NET Developer",
                    Location = "Remote",
                    Skills = "C#, ASP.NET Core, SQL, Git",
                    ExperienceSummary = "Built small ASP.NET Core projects and API integrations for coursework and internships.",
                    ResumeText = "C# ASP.NET Core SQL Git REST APIs",
                    UpdatedAtUtc = DateTime.UtcNow
                },
                new CandidateProfile
                {
                    UserId = "seed-jobseeker-2",
                    FullName = "Noah Stone",
                    Headline = "UI/UX Student Designer",
                    Location = "Hybrid",
                    Skills = "Figma, Wireframing, Responsive Design",
                    ExperienceSummary = "Designed mobile-friendly prototypes and user journeys in Figma.",
                    ResumeText = "Figma wireframing design systems prototypes accessibility responsive design",
                    UpdatedAtUtc = DateTime.UtcNow
                },
                new CandidateProfile
                {
                    UserId = "seed-jobseeker-3",
                    FullName = "Mira Adeyemi",
                    Headline = "Operations Assistant",
                    Location = "On-site",
                    Skills = "Communication, Screening, Coordination",
                    ExperienceSummary = "Supported scheduling, screening, and stakeholder communication during campus events.",
                    ResumeText = "communication screening coordination scheduling operations recruiting",
                    UpdatedAtUtc = DateTime.UtcNow
                }
            };

            dbContext.CandidateProfiles.AddRange(candidateProfiles);
            await dbContext.SaveChangesAsync();

            dbContext.JobApplications.AddRange(
                new JobApplication
                {
                    JobPostingId = jobs[0].Id,
                    ApplicantId = candidateProfiles[0].UserId,
                    ApplicantName = candidateProfiles[0].FullName,
                    ResumeUrl = string.Empty,
                    CoverLetter = "I have built ASP.NET Core projects and want to grow as a .NET developer.",
                    Status = ApplicationStatus.Accepted,
                    AppliedAtUtc = DateTime.UtcNow.AddDays(-8),
                    ReviewedAtUtc = DateTime.UtcNow.AddDays(-7),
                    ReviewerNotes = "Strong technical fit for entry-level development work."
                },
                new JobApplication
                {
                    JobPostingId = jobs[1].Id,
                    ApplicantId = candidateProfiles[0].UserId,
                    ApplicantName = candidateProfiles[0].FullName,
                    ResumeUrl = string.Empty,
                    CoverLetter = "I am interested in UI/UX but my experience is mostly backend development.",
                    Status = ApplicationStatus.Rejected,
                    AppliedAtUtc = DateTime.UtcNow.AddDays(-6),
                    ReviewedAtUtc = DateTime.UtcNow.AddDays(-5),
                    ReviewerNotes = "Profile is not a close design match."
                },
                new JobApplication
                {
                    JobPostingId = jobs[1].Id,
                    ApplicantId = candidateProfiles[1].UserId,
                    ApplicantName = candidateProfiles[1].FullName,
                    ResumeUrl = string.Empty,
                    CoverLetter = "I have worked on Figma prototypes and responsive design exercises.",
                    Status = ApplicationStatus.Accepted,
                    AppliedAtUtc = DateTime.UtcNow.AddDays(-6),
                    ReviewedAtUtc = DateTime.UtcNow.AddDays(-5),
                    ReviewerNotes = "Good design portfolio alignment."
                },
                new JobApplication
                {
                    JobPostingId = jobs[0].Id,
                    ApplicantId = candidateProfiles[1].UserId,
                    ApplicantName = candidateProfiles[1].FullName,
                    ResumeUrl = string.Empty,
                    CoverLetter = "My background is design-focused, not software development.",
                    Status = ApplicationStatus.Rejected,
                    AppliedAtUtc = DateTime.UtcNow.AddDays(-4),
                    ReviewedAtUtc = DateTime.UtcNow.AddDays(-3),
                    ReviewerNotes = "Skill set does not match the role."
                },
                new JobApplication
                {
                    JobPostingId = jobs[2].Id,
                    ApplicantId = candidateProfiles[2].UserId,
                    ApplicantName = candidateProfiles[2].FullName,
                    ResumeUrl = string.Empty,
                    CoverLetter = "I have experience with coordination, communication, and screening.",
                    Status = ApplicationStatus.Accepted,
                    AppliedAtUtc = DateTime.UtcNow.AddDays(-3),
                    ReviewedAtUtc = DateTime.UtcNow.AddDays(-2),
                    ReviewerNotes = "Strong operations and communication fit."
                },
                new JobApplication
                {
                    JobPostingId = jobs[0].Id,
                    ApplicantId = candidateProfiles[2].UserId,
                    ApplicantName = candidateProfiles[2].FullName,
                    ResumeUrl = string.Empty,
                    CoverLetter = "I am more interested in operations than software development.",
                    Status = ApplicationStatus.Rejected,
                    AppliedAtUtc = DateTime.UtcNow.AddDays(-2),
                    ReviewedAtUtc = DateTime.UtcNow.AddDays(-1),
                    ReviewerNotes = "Not aligned to the technical skill requirements."
                });

            await dbContext.SaveChangesAsync();
        }
    }
}