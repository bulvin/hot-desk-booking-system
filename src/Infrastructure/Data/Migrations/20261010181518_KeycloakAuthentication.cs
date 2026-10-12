using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    public partial class KeycloakAuthentication : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "HotDeskBooking");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "HotDeskBooking");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                schema: "HotDeskBooking",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "HotDeskBooking",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Password",
                schema: "HotDeskBooking",
                table: "Users");

            // Preserve local profiles and reservation ownership; Keycloak owns authentication.
            migrationBuilder.AlterColumn<string>(
                name: "FirstName", schema: "HotDeskBooking", table: "Users",
                type: "text", nullable: false,
                oldClrType: typeof(string), oldType: "character varying(50)", oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "LastName", schema: "HotDeskBooking", table: "Users",
                type: "text", nullable: false,
                oldClrType: typeof(string), oldType: "character varying(50)", oldMaxLength: 50);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FirstName", schema: "HotDeskBooking", table: "Users",
                type: "character varying(50)", maxLength: 50, nullable: false,
                oldClrType: typeof(string), oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LastName", schema: "HotDeskBooking", table: "Users",
                type: "character varying(50)", maxLength: 50, nullable: false,
                oldClrType: typeof(string), oldType: "text");
            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "HotDeskBooking",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                schema: "HotDeskBooking",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "HotDeskBooking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "HotDeskBooking",
                columns: table => new
                {
                    RolesId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsersId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.RolesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RolesId",
                        column: x => x.RolesId,
                        principalSchema: "HotDeskBooking",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UsersId",
                        column: x => x.UsersId,
                        principalSchema: "HotDeskBooking",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Restored credentials are placeholders; each profile needs a unique email.
            migrationBuilder.Sql(@"UPDATE ""HotDeskBooking"".""Users"" SET ""Email"" = ""Id""::text || '@rollback.invalid';");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "HotDeskBooking",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UsersId",
                schema: "HotDeskBooking",
                table: "UserRoles",
                column: "UsersId");
        }
    }
}
