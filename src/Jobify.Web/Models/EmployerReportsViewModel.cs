namespace Jobify.Web.Models;

public class EmployerReportsViewModel
{
    public string EmployerName { get; set; } = string.Empty;

    public int JobsPosted { get; set; }

    public int ActiveJobs { get; set; }

    public int TotalApplications { get; set; }

    public int ReviewBacklog { get; set; }

    public IReadOnlyList<StatusCountViewModel> ApplicationStatusCounts { get; set; } = Array.Empty<StatusCountViewModel>();

    public IReadOnlyList<CategoryCountViewModel> JobCategoryCounts { get; set; } = Array.Empty<CategoryCountViewModel>();
}