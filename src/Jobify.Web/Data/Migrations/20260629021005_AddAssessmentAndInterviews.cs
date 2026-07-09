using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobify.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentAndInterviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssessmentExpectedAnswer",
                table: "JobPostings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssessmentPrompt",
                table: "JobPostings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssessmentAnswer",
                table: "JobApplications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewLink",
                table: "JobApplications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InterviewTimeUtc",
                table: "JobApplications",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssessmentExpectedAnswer",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "AssessmentPrompt",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "AssessmentAnswer",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "InterviewLink",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "InterviewTimeUtc",
                table: "JobApplications");
        }
    }
}
