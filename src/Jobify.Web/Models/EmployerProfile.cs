namespace Jobify.Web.Models;

public class EmployerProfile
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string Industry { get; set; } = string.Empty;

    public string Website { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsApproved { get; set; } = false;

    public bool IsRejected { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; }
}