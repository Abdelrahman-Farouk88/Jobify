using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobify.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyFreelanceLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KhamsatLink",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "MostaqlLink",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "NafezlyLink",
                table: "CandidateProfiles");

            migrationBuilder.RenameColumn(
                name: "UpWorkLink",
                table: "CandidateProfiles",
                newName: "PortfolioLink2");

            migrationBuilder.RenameColumn(
                name: "OtherLinks",
                table: "CandidateProfiles",
                newName: "PortfolioLink1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PortfolioLink2",
                table: "CandidateProfiles",
                newName: "UpWorkLink");

            migrationBuilder.RenameColumn(
                name: "PortfolioLink1",
                table: "CandidateProfiles",
                newName: "OtherLinks");

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
        }
    }
}
