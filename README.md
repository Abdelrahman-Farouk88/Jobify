# Jobify

Jobify is a web-based recruitment platform built with ASP.NET Core, Entity Framework Core, Identity, SQLite, and ML.NET. It is designed to help students, fresh graduates, job seekers, startups, and employers connect through a simple hiring flow.

## Project Goal

The platform focuses on early-career hiring. It reduces noise from large general-purpose job boards by providing a cleaner experience for:

- Job seekers who want to search for relevant openings quickly
- Employers who need to post jobs and review applicants
- Administrators who need oversight of users, jobs, and applications

## Problem Statement

Many job seekers, especially students and fresh graduates, struggle to quickly find suitable job opportunities. Existing platforms are often complex, overloaded with irrelevant listings, or not focused on early-career candidates.

## Proposed Solution

Jobify provides a web-based recruitment system that lets employers post jobs and candidates search, upload CVs, and apply directly. The platform also includes an ML-assisted matching workflow that recommends jobs based on skills, resume text, and experience.

## Core Features

### Job Seeker Features

- Create and manage a profile
- Upload a real CV file in PDF, DOCX, or TXT format
- Search and filter jobs by title, category, location, and skills
- View job details and match score
- Apply directly for jobs

### Employer Features

- Create employer accounts
- Post and manage job openings
- View applications for posted jobs
- Accept, reject, or review applications
- View employer-level reporting dashboards

### Admin Features

- View platform-wide user, job, and application counts
- Monitor notifications and platform activity
- Review admin reporting dashboards

## Matching Approach

Jobify now uses a hybrid ML pipeline for recommendations.

The model is trained with ML.NET from reviewed applications and uses text features built from:

- Candidate profile text
- Extracted resume text from uploaded CV files
- Job title, category, location, and required skills

If no trained model is available yet, the app falls back to a deterministic heuristic score so the platform still works on first run.

This gives each job a match score so the most relevant jobs can be surfaced first.

## Non-Functional Requirements

### Security

- Identity-based authentication with roles
- Password hashing handled by ASP.NET Core Identity
- Protected employer and admin routes
- CV files stored in a controlled upload directory

### Performance

- Job listings are loaded from SQLite with Entity Framework Core
- Search results are ranked and filtered server-side
- Designed for responsive behavior on desktop and mobile

### Usability

- Clean dashboard-based UI
- Simple navigation for each user role
- Responsive layout using Bootstrap

## Technology Stack

- ASP.NET Core MVC
- ASP.NET Core Identity
- Entity Framework Core
- SQLite
- Bootstrap
- DocumentFormat.OpenXml for DOCX parsing
- UglyToad.PdfPig for PDF text extraction
- Microsoft.ML for text-based recommendation training and scoring

## Main Modules

- `Controllers` for job, candidate, employer, and admin flows
- `Models` for platform entities and view models
- `Services` for CV storage and job matching
- `Views` for UI pages and dashboards
- `Data` for Entity Framework context, migrations, and seeders

## Key Data Entities

- `JobPosting`
- `JobApplication`
- `CandidateProfile`
- `EmployerProfile`
- `RecruitmentNotification`

## Current Status

The project now includes:

- Role-based authentication
- Candidate profile and CV upload
- Employer posting and review workflow
- Admin oversight and reporting dashboards
- Search, filtering, and ML-ranked job recommendations

## How to Run

1. Open the solution in Visual Studio or VS Code.
2. Restore packages if needed.
3. Build the project.
4. Run the web app project in `src/Jobify.Web`.

Example:

```bash
dotnet build src/Jobify.Web/Jobify.Web.csproj
dotnet run --project src/Jobify.Web/Jobify.Web.csproj
```

## Notes

- The matching system uses ML.NET when a trained model exists and falls back to a heuristic score on first run.
- Uploaded resumes are saved under `wwwroot/uploads/resumes`.
- The project uses SQLite and EF Core migrations for schema updates.
