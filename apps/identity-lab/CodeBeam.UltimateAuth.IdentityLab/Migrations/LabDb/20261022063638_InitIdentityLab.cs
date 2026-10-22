using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeBeam.UltimateAuth.IdentityLab.Migrations.LabDb
{
    /// <inheritdoc />
    public partial class InitIdentityLab : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabSandboxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OwnerUserKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabSandboxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabSandboxMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SandboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabSandboxMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabSandboxMembers_LabSandboxes_SandboxId",
                        column: x => x.SandboxId,
                        principalTable: "LabSandboxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabSandboxSettings",
                columns: table => new
                {
                    SandboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: false, defaultValue: 5)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabSandboxSettings", x => x.SandboxId);
                    table.CheckConstraint("CK_LabSandboxSettings_MaxUsers", "\"MaxUsers\" >= 1");
                    table.ForeignKey(
                        name: "FK_LabSandboxSettings_LabSandboxes_SandboxId",
                        column: x => x.SandboxId,
                        principalTable: "LabSandboxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabSandboxes_OwnerUserKey",
                table: "LabSandboxes",
                column: "OwnerUserKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabSandboxMembers_SandboxId_Role",
                table: "LabSandboxMembers",
                columns: new[] { "SandboxId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_LabSandboxMembers_UserKey",
                table: "LabSandboxMembers",
                column: "UserKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabSandboxMembers");

            migrationBuilder.DropTable(
                name: "LabSandboxSettings");

            migrationBuilder.DropTable(
                name: "LabSandboxes");
        }
    }
}
