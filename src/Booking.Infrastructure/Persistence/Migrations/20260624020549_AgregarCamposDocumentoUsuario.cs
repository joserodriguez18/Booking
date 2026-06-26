using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposDocumentoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "fecha_nacimiento",
                table: "usuarios",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "numero_documento",
                table: "usuarios",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fecha_nacimiento",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "numero_documento",
                table: "usuarios");
        }
    }
}
