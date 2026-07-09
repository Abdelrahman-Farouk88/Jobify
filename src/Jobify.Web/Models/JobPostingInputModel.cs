using System.ComponentModel.DataAnnotations;

namespace Jobify.Web.Models;

public class JobPostingInputModel
{
    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Category { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Location { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string RequiredSkills { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string EmploymentType { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string ExperienceLevel { get; set; } = string.Empty;

    [StringLength(500)]
    public string? AssessmentPrompt { get; set; }

    [StringLength(500)]
    public string? AssessmentExpectedAnswer { get; set; }
}