using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobify.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateResumeStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ResumeUrl",
                table: "CandidateProfiles",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "ResumeContentType",
                table: "CandidateProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeFileName",
                table: "CandidateProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeStoredPath",
                table: "CandidateProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeText",
                table: "CandidateProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResumeUploadedAtUtc",
                table: "CandidateProfiles",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResumeContentType",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ResumeFileName",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ResumeStoredPath",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ResumeText",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ResumeUploadedAtUtc",
                table: "CandidateProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "ResumeUrl",
                table: "CandidateProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
