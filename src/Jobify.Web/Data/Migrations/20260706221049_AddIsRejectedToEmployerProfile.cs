using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobify.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsRejectedToEmployerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRejected",
                table: "EmployerProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRejected",
                table: "EmployerProfiles");
        }
    }
}
