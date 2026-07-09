using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Jobify.Web.Models;

namespace Jobify.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<IdentityUser, IdentityRole, string>(options)
{
	public DbSet<JobPosting> JobPostings => Set<JobPosting>();

	public DbSet<JobApplication> JobApplications => Set<JobApplication>();

	public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();

	public DbSet<EmployerProfile> EmployerProfiles => Set<EmployerProfile>();

	public DbSet<RecruitmentNotification> RecruitmentNotifications => Set<RecruitmentNotification>();
}
