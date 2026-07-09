namespace Jobify.Web.Models;

public class JobMatchResultViewModel
{
    public decimal? MatchScore { get; set; }

    public IReadOnlyList<string> MatchReasons { get; set; } = Array.Empty<string>();

    public string MatchSource { get; set; } = string.Empty;
}