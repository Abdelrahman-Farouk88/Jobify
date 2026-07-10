using System.Text.RegularExpressions;
using Jobify.Web.Data;
using Jobify.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Jobify.Web.Services;

public class JobMatchService(ApplicationDbContext dbContext, IWebHostEnvironment environment)
{
    private static readonly MLContext MlContext = new(seed: 42);

    private static readonly object ModelLock = new();

    private static ITransformer? cachedModel;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "the", "for", "with", "from", "into", "your", "you", "that", "this", "have", "will", "are", "was", "were", "our", "their", "they", "them", "job", "role", "work", "skills", "experience"
    };

    private string ModelPath => Path.Combine(environment.ContentRootPath, "App_Data", "Models", "job-match.zip");

    public async Task TrainAsync(CancellationToken cancellationToken = default)
    {
        var trainingRows = await BuildTrainingRowsAsync(cancellationToken);
        if (trainingRows.Count < 6)
        {
            return;
        }

        var dataView = MlContext.Data.LoadFromEnumerable(trainingRows);
        var pipeline = MlContext.Transforms.Text.FeaturizeText("CandidateFeatures", nameof(JobMatchTrainingSample.CandidateText))
            .Append(MlContext.Transforms.Text.FeaturizeText("JobFeatures", nameof(JobMatchTrainingSample.JobText)))
            .Append(MlContext.Transforms.Concatenate("Features", "CandidateFeatures", "JobFeatures"))
            .Append(MlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: nameof(JobMatchTrainingSample.Label), featureColumnName: "Features"));

        var model = pipeline.Fit(dataView);
        Directory.CreateDirectory(Path.GetDirectoryName(ModelPath)!);
        MlContext.Model.Save(model, dataView.Schema, ModelPath);

        lock (ModelLock)
        {
            cachedModel = model;
        }
    }

    public JobMatchResultViewModel Score(CandidateProfile? candidate, JobPosting job)
    {
        if (candidate is null)
        {
            return new JobMatchResultViewModel
            {
                MatchScore = null,
                MatchReasons = Array.Empty<string>(),
                MatchSource = "Heuristic"
            };
        }

        var heuristic = BuildHeuristicResult(candidate, job);
        var model = GetModel();

        if (model is null)
        {
            return heuristic;
        }

        var predictionEngine = MlContext.Model.CreatePredictionEngine<JobMatchTrainingSample, JobMatchPrediction>(model);
        var prediction = predictionEngine.Predict(new JobMatchTrainingSample
        {
            CandidateText = BuildCandidateText(candidate),
            JobText = BuildJobText(job)
        });

        var mlScore = Math.Round((decimal)Math.Clamp(prediction.Probability, 0f, 1f) * 100m, 0);
        var heuristicScore = heuristic.MatchScore ?? 0;

        var finalScore = (mlScore + heuristicScore) / 2;

        return new JobMatchResultViewModel
        {
            MatchScore = Math.Round(finalScore, 0),
            MatchReasons = heuristic.MatchReasons,
            MatchSource = "ML"
        };
    }

    private async Task<List<JobMatchTrainingSample>> BuildTrainingRowsAsync(CancellationToken cancellationToken)
    {
        var reviewedApplications = await dbContext.JobApplications
            .AsNoTracking()
            .Where(application => application.Status == ApplicationStatus.Accepted || application.Status == ApplicationStatus.Rejected)
            .Select(application => new
            {
                application.ApplicantId,
                application.JobPostingId,
                application.Status
            })
            .ToListAsync(cancellationToken);

        if (reviewedApplications.Count == 0)
        {
            return [];
        }

        var candidateProfiles = await dbContext.CandidateProfiles
            .AsNoTracking()
            .ToDictionaryAsync(profile => profile.UserId, cancellationToken);

        var jobs = await dbContext.JobPostings
            .AsNoTracking()
            .ToDictionaryAsync(job => job.Id, cancellationToken);

        var trainingRows = new List<JobMatchTrainingSample>();

        foreach (var application in reviewedApplications)
        {
            if (!candidateProfiles.TryGetValue(application.ApplicantId, out var candidateProfile))
            {
                continue;
            }

            if (!jobs.TryGetValue(application.JobPostingId, out var job))
            {
                continue;
            }

            trainingRows.Add(new JobMatchTrainingSample
            {
                Label = application.Status == ApplicationStatus.Accepted,
                CandidateText = BuildCandidateText(candidateProfile),
                JobText = BuildJobText(job)
            });
        }

        return trainingRows;
    }

    private ITransformer? GetModel()
    {
        lock (ModelLock)
        {
            if (cachedModel is not null)
            {
                return cachedModel;
            }

            if (!File.Exists(ModelPath))
            {
                return null;
            }

            cachedModel = MlContext.Model.Load(ModelPath, out _);
            return cachedModel;
        }
    }

    private static JobMatchResultViewModel BuildHeuristicResult(CandidateProfile candidate, JobPosting job)
    {
        var candidateText = BuildCandidateText(candidate);
        var candidateTokens = Tokenize(candidateText);
        var jobTokens = Tokenize(BuildJobText(job));
        var totalJobKeywords = jobTokens.Count;

        var skillTokens = Tokenize(job.RequiredSkills);
        var candidateSkillTokens = Tokenize(candidate.Skills);

        // Calculate Skill Match Score (MAX 50 points - most important)
        int skillMatches = 0;
        double skillScore = 0;
        if (skillTokens.Count > 0 && candidateSkillTokens.Count > 0)
        {
            skillMatches = skillTokens.Intersect(candidateSkillTokens).Count();
            double skillMatchRatio = (double)skillMatches / skillTokens.Count;
            // Only give points if there are actual matches
            if (skillMatches > 0)
            {
                // Use exponential to make it harder to get high scores
                skillScore = Math.Pow(skillMatchRatio, 2.5) * 50;
            }
        }
        else if (skillTokens.Count > 0 && candidateSkillTokens.Count == 0)
        {
            skillScore = 0;
        }

        // Calculate Title Match (MAX 15 points)
        var jobTitleTokens = Tokenize(job.Title);
        var titleMatches = jobTitleTokens.Intersect(candidateTokens).Count();
        double titleScore = 0;
        if (jobTitleTokens.Count > 0 && titleMatches > 0)
        {
            double titleMatchRatio = (double)titleMatches / jobTitleTokens.Count;
            titleScore = titleMatchRatio * 15;
        }

        // Calculate Location Match (MAX 15 points)
        double locationScore = 0;
        if (!string.IsNullOrEmpty(candidate.Location) && !string.IsNullOrEmpty(job.Location))
        {
            var candidateLocation = candidate.Location.Trim().ToLowerInvariant();
            var jobLocation = job.Location.Trim().ToLowerInvariant();

            if (candidateLocation == jobLocation)
            {
                locationScore = 15;
            }
            else if (candidateLocation.Contains(jobLocation) || jobLocation.Contains(candidateLocation))
            {
                locationScore = 10;
            }
            else
            {
                // Check if both are remote/hybrid
                bool isRemote = jobLocation.Contains("remote") || candidateLocation.Contains("remote");
                bool isHybrid = jobLocation.Contains("hybrid") || candidateLocation.Contains("hybrid");
                if (isRemote || isHybrid)
                {
                    locationScore = 5;
                }
            }
        }

        // Calculate Experience Match (MAX 15 points)
        double experienceScore = 0;
        if (!string.IsNullOrEmpty(job.ExperienceLevel))
        {
            var jobExperience = job.ExperienceLevel.ToLowerInvariant();
            var candidateExperience = (candidate.ExperienceSummary + " " + candidate.Headline).ToLowerInvariant();

            if (candidateExperience.Contains(jobExperience))
            {
                experienceScore = 15;
            }
            else
            {
                // Check for related keywords
                var experienceKeywords = new[] { "junior", "entry", "senior", "lead", "manager", "director", "intern", "trainee" };
                foreach (var keyword in experienceKeywords)
                {
                    if (jobExperience.Contains(keyword) && candidateExperience.Contains(keyword))
                    {
                        experienceScore = 10;
                        break;
                    }
                }
            }
        }
        else
        {
            experienceScore = 15; // No experience required
        }

        // Calculate Keyword Overlap (MAX 5 points - least important)
        double keywordScore = 0;
        if (totalJobKeywords > 0)
        {
            var keywordOverlaps = jobTokens.Intersect(candidateTokens).Count();
            if (keywordOverlaps > 0)
            {
                double keywordMatchRatio = (double)keywordOverlaps / totalJobKeywords;
                keywordScore = Math.Min(keywordMatchRatio * 5, 5);
            }
        }

        // Calculate total score
        double totalScore = skillScore + titleScore + locationScore + experienceScore + keywordScore;
        totalScore = Math.Min(100, Math.Max(0, Math.Round(totalScore, 0)));

        // Build reasons
        var reasons = new List<string>();

        // Skill match reasons
        if (skillMatches > 0 && skillTokens.Count > 0)
        {
            if (skillMatches == skillTokens.Count)
            {
                reasons.Add($"All {skillTokens.Count} required skills matched");
            }
            else
            {
                reasons.Add($"{skillMatches} of {skillTokens.Count} required skills matched");
                var missingSkills = skillTokens.Except(candidateSkillTokens).Take(3);
                if (missingSkills.Any())
                {
                    reasons.Add($"Missing: {string.Join(", ", missingSkills)}");
                }
            }
        }
        else if (skillTokens.Count > 0 && skillMatches == 0)
        {
            reasons.Add($"No matching skills found");
            reasons.Add($"Required: {string.Join(", ", skillTokens.Take(3))}");
        }

        // Title match
        if (titleMatches > 0)
        {
            reasons.Add($"{titleMatches} job title keyword{(titleMatches > 1 ? "s" : "")} matched");
        }

        // Location match
        if (locationScore >= 15)
        {
            reasons.Add($"Location matches: {job.Location}");
        }
        else if (locationScore >= 10)
        {
            reasons.Add($"Location partially matches: {job.Location}");
        }
        else if (!string.IsNullOrEmpty(job.Location) && locationScore == 0)
        {
            reasons.Add($"Location mismatch: {job.Location}");
        }

        // Experience match
        if (experienceScore >= 15)
        {
            reasons.Add($"Experience level matches: {job.ExperienceLevel}");
        }
        else if (experienceScore >= 10)
        {
            reasons.Add($"Experience level partially matches: {job.ExperienceLevel}");
        }
        else if (!string.IsNullOrEmpty(job.ExperienceLevel))
        {
            reasons.Add($"Experience mismatch: {job.ExperienceLevel} required");
        }

        // Keyword overlap
        if (keywordScore > 0 && skillMatches == 0)
        {
            reasons.Add($"Keyword overlaps found in resume");
        }

        // Overall rating
        if (totalScore >= 80)
        {
            reasons.Add("Excellent match!");
        }
        else if (totalScore >= 60)
        {
            reasons.Add("Good match");
        }
        else if (totalScore >= 40)
        {
            reasons.Add("Moderate match - consider updating skills");
        }
        else if (totalScore >= 20)
        {
            reasons.Add("Low match - significant skill gap");
        }
        else
        {
            reasons.Add("Very low match - skills don't align");
        }

        return new JobMatchResultViewModel
        {
            MatchScore = (decimal)totalScore,
            MatchReasons = reasons,
            MatchSource = "Heuristic"
        };
    }

    private static string BuildCandidateText(CandidateProfile candidate)
    {
        return string.Join(' ', new[]
        {
            candidate.FullName,
            candidate.Headline,
            candidate.Skills,
            candidate.ExperienceSummary,
            candidate.ResumeText
        }.Where(value => !string.IsNullOrWhiteSpace(value))!);
    }

    private static string BuildJobText(JobPosting job)
    {
        return string.Join(' ', job.Title, job.Category, job.RequiredSkills, job.Description, job.ExperienceLevel, job.Location);
    }

    private static HashSet<string> Tokenize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return Regex.Split(value.ToLowerInvariant(), "[^a-z0-9#+]+")
            .Where(token => token.Length > 2 && !StopWords.Contains(token))
            .ToHashSet();
    }

    private sealed class JobMatchTrainingSample
    {
        public bool Label { get; set; }

        public string CandidateText { get; set; } = string.Empty;

        public string JobText { get; set; } = string.Empty;
    }

    private sealed class JobMatchPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool PredictedLabel { get; set; }

        public float Score { get; set; }

        public float Probability { get; set; }
    }
}