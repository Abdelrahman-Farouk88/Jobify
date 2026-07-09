namespace Jobify.Web.Models;

public class EmployerDashboardViewModel
{
    public IReadOnlyList<JobPosting> Jobs { get; set; } = Array.Empty<JobPosting>();

    public IReadOnlyList<JobApplication> Applications { get; set; } = Array.Empty<JobApplication>();

    public int OpenJobs { get; set; }

    public int TotalApplications { get; set; }

    public int ReviewedApplications { get; set; }

    public int SubmittedApplications { get; set; }
}