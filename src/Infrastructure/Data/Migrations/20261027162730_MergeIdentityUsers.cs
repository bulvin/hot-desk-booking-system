using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MergeIdentityUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Users_UserId",
                schema: "HotDeskBooking",
                table: "Reservations");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "HotDeskBooking",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "HotDeskBooking",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "HotDeskBooking",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "HotDeskBooking",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "HotDeskBooking",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Preserve credentials and roles for existing accounts; merge only their booking profile fields.
            migrationBuilder.Sql("""
                UPDATE "HotDeskBooking"."AspNetUsers" AS account
                SET "FirstName" = profile."FirstName", "LastName" = profile."LastName",
                    "CreatedAt" = profile."CreatedAt", "UpdatedAt" = profile."UpdatedAt"
                FROM "HotDeskBooking"."Users" AS profile
                WHERE account."Id" = profile."Id";

                INSERT INTO "HotDeskBooking"."AspNetUsers"
                    ("Id", "Email", "FirstName", "LastName", "CreatedAt", "UpdatedAt", "IsActive",
                     "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount",
                     "SecurityStamp", "ConcurrencyStamp")
                SELECT profile."Id", profile."Email", profile."FirstName", profile."LastName",
                       profile."CreatedAt", profile."UpdatedAt", false, false, false, false, true, 0,
                       gen_random_uuid()::text, gen_random_uuid()::text
                FROM "HotDeskBooking"."Users" AS profile
                WHERE NOT EXISTS (
                    SELECT 1 FROM "HotDeskBooking"."AspNetUsers" AS account WHERE account."Id" = profile."Id");
                """);

            migrationBuilder.DropTable(
                name: "Users",
                schema: "HotDeskBooking");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_AspNetUsers_UserId",
                schema: "HotDeskBooking",
                table: "Reservations",
                column: "UserId",
                principalSchema: "HotDeskBooking",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_AspNetUsers_UserId",
                schema: "HotDeskBooking",
                table: "Reservations");

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "HotDeskBooking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.Sql("""
                INSERT INTO "HotDeskBooking"."Users" ("Id", "Email", "FirstName", "LastName", "CreatedAt", "UpdatedAt")
                SELECT "Id", "Email", "FirstName", "LastName", "CreatedAt", "UpdatedAt"
                FROM "HotDeskBooking"."AspNetUsers";
                """);

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "HotDeskBooking",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "HotDeskBooking",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "HotDeskBooking",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "HotDeskBooking",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "HotDeskBooking",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Users_UserId",
                schema: "HotDeskBooking",
                table: "Reservations",
                column: "UserId",
                principalSchema: "HotDeskBooking",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
