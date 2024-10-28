using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaFisTransferEntegrasyonu.Migrations
{
    /// <inheritdoc />
    public partial class AddUstIslemTuru : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ust_islem_turu",
                table: "users",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ust_islem_turu",
                table: "users");
        }
    }
}
