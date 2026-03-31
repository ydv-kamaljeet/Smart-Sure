using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSure.Claims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStartEndDateToValidPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "ValidPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "ValidPolicies",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "ValidPolicies");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "ValidPolicies");
        }
    }
}
