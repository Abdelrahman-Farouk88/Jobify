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

        return new JobMatchResultViewModel
        {
            MatchScore = Math.Round((decimal)Math.Clamp(prediction.Probability, 0f, 1f) * 100m, 0),
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

        var skillTokens = Tokenize(job.RequiredSkills);
        var candidateSkillTokens = Tokenize(candidate.Skills);

        var skillMatches = skillTokens.Intersect(candidateSkillTokens).Count();
        var resumeMatches = jobTokens.Intersect(candidateTokens).Count();
        var titleMatches = Tokenize(job.Title).Intersect(candidateTokens).Count();
        var locationMatch = string.Equals(candidate.Location.Trim(), job.Location.Trim(), StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        var skillScore = skillTokens.Count == 0 ? 0 : skillMatches * 40m / skillTokens.Count;
        var resumeScore = jobTokens.Count == 0 ? 0 : resumeMatches * 25m / jobTokens.Count;
        var titleScore = Tokenize(job.Title).Count == 0 ? 0 : titleMatches * 10m / Math.Max(Tokenize(job.Title).Count, 1);
        var locationScore = locationMatch * 5m;

        var baseScore = (candidateTokens.Count > 0) ? 20m : 0m;

        var totalScore = Math.Round(baseScore + skillScore + resumeScore + titleScore + locationScore, 0);
        totalScore = Math.Min(100m, Math.Max(0m, totalScore));

        var reasons = new List<string>();
        if (skillMatches > 0)
        {
            reasons.Add($"{skillMatches} required skill match{(skillMatches == 1 ? string.Empty : "es")}");
        }

        if (resumeMatches > 0)
        {
            reasons.Add($"{resumeMatches} keyword overlap{(resumeMatches == 1 ? string.Empty : "s")} in resume text");
        }

        if (locationMatch == 1)
        {
            reasons.Add("Preferred location matched");
        }

        return new JobMatchResultViewModel
        {
            MatchScore = totalScore,
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