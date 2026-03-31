using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSure.Claims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimNumberToClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaimNumber",
                table: "Claims",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaimNumber",
                table: "Claims");
        }
    }
}
