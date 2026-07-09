namespace Jobify.Web.Models;

public enum JobStatus
{
    Pending,
    Approved,
    Rejected
}

public class JobPosting
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string RequiredSkills { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string EmploymentType { get; set; } = string.Empty;

    public string ExperienceLevel { get; set; } = string.Empty;

    public string EmployerName { get; set; } = string.Empty;

    public string? EmployerId { get; set; }

    public DateTime PostedAtUtc { get; set; }

    public bool IsActive { get; set; }

    public string? AssessmentPrompt { get; set; }

    public string? AssessmentExpectedAnswer { get; set; }

    public string? RejectionReason { get; set; }

    public JobStatus Status { get; set; } = JobStatus.Pending;
}