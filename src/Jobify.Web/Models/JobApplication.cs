namespace Jobify.Web.Models;

public class JobApplication
{
    public int Id { get; set; }

    public int JobPostingId { get; set; }

    public JobPosting? JobPosting { get; set; }

    public string ApplicantId { get; set; } = string.Empty;

    public string ApplicantName { get; set; } = string.Empty;

    public string ResumeUrl { get; set; } = string.Empty;

    public string CoverLetter { get; set; } = string.Empty;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.UnderReview;

    public DateTime AppliedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string? ReviewerNotes { get; set; }

    public bool IsShortlisted { get; set; }

    public string? AssessmentAnswer { get; set; }

    public string? InterviewLink { get; set; }

    public DateTime? InterviewTimeUtc { get; set; }
}