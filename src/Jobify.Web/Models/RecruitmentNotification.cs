namespace Jobify.Web.Models;

public class RecruitmentNotification
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}