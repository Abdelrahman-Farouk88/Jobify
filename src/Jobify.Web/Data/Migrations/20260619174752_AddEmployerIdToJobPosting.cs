using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobify.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployerIdToJobPosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployerId",
                table: "JobPostings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployerId",
                table: "JobPostings");
        }
    }
}
