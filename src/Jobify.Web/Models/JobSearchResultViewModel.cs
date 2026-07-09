namespace Jobify.Web.Models;

public class JobSearchResultViewModel
{
    public JobPosting Job { get; set; } = new();

    public decimal? MatchScore { get; set; }

    public IReadOnlyList<string> MatchReasons { get; set; } = Array.Empty<string>();

    public string MatchSource { get; set; } = string.Empty;
}