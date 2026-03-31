using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSure.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyIdToAdminClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PolicyId",
                table: "AdminClaims",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PolicyId",
                table: "AdminClaims");
        }
    }
}
