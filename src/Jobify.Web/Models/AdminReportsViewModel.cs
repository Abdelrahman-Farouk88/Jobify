namespace Jobify.Web.Models;

public class AdminReportsViewModel
{
    public int TotalUsers { get; set; }

    public int JobSeekers { get; set; }

    public int Employers { get; set; }

    public int Jobs { get; set; }

    public int Applications { get; set; }

    public IReadOnlyList<StatusCountViewModel> ApplicationStatusCounts { get; set; } = Array.Empty<StatusCountViewModel>();

    public IReadOnlyList<CategoryCountViewModel> JobCategoryCounts { get; set; } = Array.Empty<CategoryCountViewModel>();
}