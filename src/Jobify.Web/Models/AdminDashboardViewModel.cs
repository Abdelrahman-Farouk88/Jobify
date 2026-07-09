namespace Jobify.Web.Models;

public class AdminDashboardViewModel
{
    public int JobSeekerCount { get; set; }

    public int EmployerCount { get; set; }

    public int JobCount { get; set; }

    public int ApplicationCount { get; set; }

    public int NotificationCount { get; set; }

    public int ActiveJobs { get; set; }

    public int InactiveJobs { get; set; }

    public int SubmittedApplications { get; set; }

    public int ReviewedApplications { get; set; }

    public List<JobPosting> PendingJobs { get; set; } = new();

    public List<EmployerApprovalViewModel> PendingEmployers { get; set; } = new();

    public List<ApplicationUserViewModel> Users { get; set; } = new();
}

public class EmployerApprovalViewModel
{
    public int ProfileId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}

public class ApplicationUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}