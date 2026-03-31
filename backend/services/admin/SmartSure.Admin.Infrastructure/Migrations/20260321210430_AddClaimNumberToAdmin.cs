using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSure.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimNumberToAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaimNumber",
                table: "AdminClaims",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaimNumber",
                table: "AdminClaims");
        }
    }
}
