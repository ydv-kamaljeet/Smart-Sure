using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSure.Identity.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCountryColumnFromUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
