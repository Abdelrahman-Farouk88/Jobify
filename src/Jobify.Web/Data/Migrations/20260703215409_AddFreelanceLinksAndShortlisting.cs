using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobify.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFreelanceLinksAndShortlisting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsShortlisted",
                table: "JobApplications",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KhamsatLink",
                table: "CandidateProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MostaqlLink",
                table: "CandidateProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NafezlyLink",
                table: "CandidateProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherLinks",
                table: "CandidateProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpWorkLink",
                table: "CandidateProfiles",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsShortlisted",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "KhamsatLink",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "MostaqlLink",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "NafezlyLink",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "OtherLinks",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "UpWorkLink",
                table: "CandidateProfiles");
        }
    }
}
