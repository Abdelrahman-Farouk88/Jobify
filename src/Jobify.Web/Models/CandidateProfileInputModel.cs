using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Jobify.Web.Models;

public class CandidateProfileInputModel
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string Headline { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Location { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string Skills { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string ExperienceSummary { get; set; } = string.Empty;

    public IFormFile? ResumeFile { get; set; }

    public string? CurrentResumeFileName { get; set; }

    public string? CurrentResumeUrl { get; set; }

    [System.ComponentModel.DataAnnotations.Url]
    public string? PortfolioLink1 { get; set; }

    [System.ComponentModel.DataAnnotations.Url]
    public string? PortfolioLink2 { get; set; }
}