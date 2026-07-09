namespace Jobify.Web.Models;

public class JobDetailsViewModel
{
    public JobPosting Job { get; set; } = new();

    public bool HasApplied { get; set; }

    public decimal? MatchScore { get; set; }

    public IReadOnlyList<string> MatchReasons { get; set; } = Array.Empty<string>();

    public string MatchSource { get; set; } = string.Empty;
}