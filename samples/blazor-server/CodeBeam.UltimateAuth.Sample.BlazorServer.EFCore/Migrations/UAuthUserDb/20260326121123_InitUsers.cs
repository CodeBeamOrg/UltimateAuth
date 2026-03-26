using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Migrations.UAuthUserDb
{
    /// <inheritdoc />
    public partial class InitUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UAuth_UserIdentifiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    NormalizedValue = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_UserIdentifiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UAuth_UserLifecycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SecurityVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_UserLifecycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UAuth_UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Gender = table.Column<string>(type: "TEXT", nullable: true),
                    Bio = table.Column<string>(type: "TEXT", nullable: true),
                    Language = table.Column<string>(type: "TEXT", nullable: true),
                    TimeZone = table.Column<string>(type: "TEXT", nullable: true),
                    Culture = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserIdentifiers_Tenant_NormalizedValue",
                table: "UAuth_UserIdentifiers",
                columns: new[] { "Tenant", "NormalizedValue" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserIdentifiers_Tenant_Type_NormalizedValue",
                table: "UAuth_UserIdentifiers",
                columns: new[] { "Tenant", "Type", "NormalizedValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserIdentifiers_Tenant_UserKey",
                table: "UAuth_UserIdentifiers",
                columns: new[] { "Tenant", "UserKey" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserIdentifiers_Tenant_UserKey_IsPrimary",
                table: "UAuth_UserIdentifiers",
                columns: new[] { "Tenant", "UserKey", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserIdentifiers_Tenant_UserKey_Type_IsPrimary",
                table: "UAuth_UserIdentifiers",
                columns: new[] { "Tenant", "UserKey", "Type", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserLifecycles_Tenant_UserKey",
                table: "UAuth_UserLifecycles",
                columns: new[] { "Tenant", "UserKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserProfiles_Tenant_UserKey",
                table: "UAuth_UserProfiles",
                columns: new[] { "Tenant", "UserKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UAuth_UserIdentifiers");

            migrationBuilder.DropTable(
                name: "UAuth_UserLifecycles");

            migrationBuilder.DropTable(
                name: "UAuth_UserProfiles");
        }
    }
}
