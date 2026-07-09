namespace Jobify.Web.Models;

public class JobSearchViewModel
{
    public string? Search { get; set; }

    public string? Category { get; set; }

    public string? Location { get; set; }

    public string? Skill { get; set; }

    public IReadOnlyList<JobSearchResultViewModel> Jobs { get; set; } = Array.Empty<JobSearchResultViewModel>();

    public IReadOnlyList<string> Categories { get; set; } = Array.Empty<string>();
}