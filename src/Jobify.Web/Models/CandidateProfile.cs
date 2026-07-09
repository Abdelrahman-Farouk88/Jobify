namespace Jobify.Web.Models;

public class CandidateProfile
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Headline { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Skills { get; set; } = string.Empty;

    public string ExperienceSummary { get; set; } = string.Empty;

    public string? ResumeFileName { get; set; }

    public string? ResumeStoredPath { get; set; }

    public string? ResumeContentType { get; set; }

    public string? ResumeText { get; set; }

    public string? ResumeUrl { get; set; }

    public DateTime? ResumeUploadedAtUtc { get; set; }

    public string? PortfolioLink1 { get; set; }

    public string? PortfolioLink2 { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}