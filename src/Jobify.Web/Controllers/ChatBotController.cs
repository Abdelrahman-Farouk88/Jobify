using Jobify.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatBotController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPost("Ask")]
    public async Task<IActionResult> Ask([FromBody] ChatRequest request)
    {
        var message = request.Message?.ToLowerInvariant() ?? "";

        // Simple Keyword-based AI Simulation
        var allJobs = await dbContext.JobPostings
            .AsNoTracking()
            .Where(j => j.IsActive)
            .ToListAsync();

        var matchedJobs = allJobs
            .Where(j => 
                (j.Title != null && message.Contains(j.Title.ToLowerInvariant())) ||
                (j.Description != null && message.Contains(j.Description.ToLowerInvariant())) ||
                (j.Category != null && message.Contains(j.Category.ToLowerInvariant())) ||
                (j.RequiredSkills != null && message.Contains(j.RequiredSkills.ToLowerInvariant()))
            )
            .Take(3)
            .ToList();

        // If no strict matches, just return random/latest jobs as suggestions
        if (!matchedJobs.Any())
        {
            matchedJobs = allJobs.OrderByDescending(j => j.PostedAtUtc).Take(2).ToList();
            if (!matchedJobs.Any())
            {
                return Ok(new { text = "I couldn't find any active jobs right now, sorry! Please try again later.", jobs = new List<object>() });
            }

            var fallbackResponse = new
            {
                text = "I couldn't find an exact match for that, but here are some of our latest openings:",
                jobs = matchedJobs.Select(j => new { id = j.Id, title = j.Title, employer = j.EmployerName })
            };
            return Ok(fallbackResponse);
        }

        var response = new
        {
            text = $"I found {matchedJobs.Count} job(s) that might match your criteria! Check these out:",
            jobs = matchedJobs.Select(j => new { id = j.Id, title = j.Title, employer = j.EmployerName })
        };

        return Ok(response);
    }
}

public class ChatRequest
{
    public string? Message { get; set; }
}
