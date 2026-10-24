using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeBeam.UltimateAuth.IdentityLab.Migrations.LabDb
{
    /// <inheritdoc />
    public partial class AddActivityLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SandboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TargetUserKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabActivityLogs_LabSandboxes_SandboxId",
                        column: x => x.SandboxId,
                        principalTable: "LabSandboxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabActivityLogs_SandboxId_ActorUserKey",
                table: "LabActivityLogs",
                columns: new[] { "SandboxId", "ActorUserKey" });

            migrationBuilder.CreateIndex(
                name: "IX_LabActivityLogs_SandboxId_CreatedAt",
                table: "LabActivityLogs",
                columns: new[] { "SandboxId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabActivityLogs");
        }
    }
}
