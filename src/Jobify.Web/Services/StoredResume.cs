namespace Jobify.Web.Services;

public class StoredResume
{
    public string FileName { get; set; } = string.Empty;

    public string StoredPath { get; set; } = string.Empty;

    public string RelativeUrl { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string ExtractedText { get; set; } = string.Empty;
}