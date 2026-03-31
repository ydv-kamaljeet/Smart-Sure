using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSure.Claims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyNumberToValidPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PolicyNumber",
                table: "ValidPolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PolicyNumber",
                table: "ValidPolicies");
        }
    }
}
