using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Migrations.UAuthAuthorizationDb
{
    /// <inheritdoc />
    public partial class InitAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UAuth_RolePermissions",
                columns: table => new
                {
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Permission = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_RolePermissions", x => new { x.Tenant, x.RoleId, x.Permission });
                });

            migrationBuilder.CreateTable(
                name: "UAuth_Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UAuth_UserRoles",
                columns: table => new
                {
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_UserRoles", x => new { x.Tenant, x.UserKey, x.RoleId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RolePermissions_Tenant_Permission",
                table: "UAuth_RolePermissions",
                columns: new[] { "Tenant", "Permission" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RolePermissions_Tenant_RoleId",
                table: "UAuth_RolePermissions",
                columns: new[] { "Tenant", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Roles_Tenant_Id",
                table: "UAuth_Roles",
                columns: new[] { "Tenant", "Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Roles_Tenant_NormalizedName",
                table: "UAuth_Roles",
                columns: new[] { "Tenant", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserRoles_Tenant_RoleId",
                table: "UAuth_UserRoles",
                columns: new[] { "Tenant", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserRoles_Tenant_UserKey",
                table: "UAuth_UserRoles",
                columns: new[] { "Tenant", "UserKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UAuth_RolePermissions");

            migrationBuilder.DropTable(
                name: "UAuth_Roles");

            migrationBuilder.DropTable(
                name: "UAuth_UserRoles");
        }
    }
}
