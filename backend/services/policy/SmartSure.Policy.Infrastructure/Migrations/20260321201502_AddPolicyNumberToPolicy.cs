using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSure.Policy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyNumberToPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PolicyNumber",
                table: "Policies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PolicyNumber",
                table: "Policies");
        }
    }
}
