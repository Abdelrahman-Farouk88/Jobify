using System.IO.Compression;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Hosting;
using UglyToad.PdfPig;

namespace Jobify.Web.Services;

public class ResumeStorageService(IWebHostEnvironment environment)
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx",
        ".txt"
    };

    public async Task<StoredResume> SaveAsync(IFormFile resumeFile, string candidateUserId, string? previousStoredPath = null)
    {
        var extension = Path.GetExtension(resumeFile.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only PDF, DOCX, and TXT resumes are supported.");
        }

        var resumeFolder = Path.Combine(environment.WebRootPath, "uploads", "resumes", candidateUserId);
        Directory.CreateDirectory(resumeFolder);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var storedPath = Path.Combine(resumeFolder, storedFileName);

        await using (var fileStream = new FileStream(storedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await resumeFile.CopyToAsync(fileStream);
        }

        var extractedText = await ExtractTextAsync(storedPath, extension);

        if (!string.IsNullOrWhiteSpace(previousStoredPath) && File.Exists(previousStoredPath))
        {
            File.Delete(previousStoredPath);
        }

        return new StoredResume
        {
            FileName = Path.GetFileName(resumeFile.FileName),
            StoredPath = storedPath,
            RelativeUrl = $"/uploads/resumes/{candidateUserId}/{storedFileName}",
            ContentType = resumeFile.ContentType,
            ExtractedText = extractedText
        };
    }

    private static async Task<string> ExtractTextAsync(string storedPath, string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".txt" => await File.ReadAllTextAsync(storedPath),
            ".docx" => ExtractWordText(storedPath),
            ".pdf" => ExtractPdfText(storedPath),
            _ => string.Empty
        };
    }

    private static string ExtractWordText(string storedPath)
    {
        using var document = WordprocessingDocument.Open(storedPath, false);
        var body = document.MainDocumentPart?.Document.Body;
        if (body is null)
        {
            return string.Empty;
        }

        return string.Join(" ", body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(text => text.Text));
    }

    private static string ExtractPdfText(string storedPath)
    {
        using var stream = File.OpenRead(storedPath);
        using var document = PdfDocument.Open(stream);
        var builder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }
}