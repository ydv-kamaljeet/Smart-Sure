using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSure.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeClaimIdToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ClaimId",
                table: "AdminClaims");

            migrationBuilder.AddColumn<int>(
                name: "ClaimId",
                table: "AdminClaims",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ClaimId",
                table: "AdminClaims",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });
        }
    }
}
