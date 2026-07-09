using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobify.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJobStatusAndEmployerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "JobPostings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "JobPostings");
        }
    }
}
