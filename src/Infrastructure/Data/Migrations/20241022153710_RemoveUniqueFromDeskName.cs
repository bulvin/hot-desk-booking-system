using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueFromDeskName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Desks_Name",
                schema: "HotDeskBooking",
                table: "Desks");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "HotDeskBooking",
                table: "Desks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "HotDeskBooking",
                table: "Desks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_Desks_Name",
                schema: "HotDeskBooking",
                table: "Desks",
                column: "Name",
                unique: true);
        }
    }
}
