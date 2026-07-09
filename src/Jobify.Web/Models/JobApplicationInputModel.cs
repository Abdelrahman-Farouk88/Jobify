using System.ComponentModel.DataAnnotations;

namespace Jobify.Web.Models;

public class JobApplicationInputModel
{
    public int JobPostingId { get; set; }

    [Required, StringLength(250)]
    public string ResumeUrl { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string CoverLetter { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? AssessmentAnswer { get; set; }
}