using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RastreadorHabitos.Core.ControlAcceso.Migrations
{
    /// <inheritdoc />
    public partial class BloqueoPorIntentosFallidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BloqueadoHastaUtc",
                schema: "ControlAcceso",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntentosFallidosConsecutivos",
                schema: "ControlAcceso",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BloqueadoHastaUtc",
                schema: "ControlAcceso",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "IntentosFallidosConsecutivos",
                schema: "ControlAcceso",
                table: "Usuarios");
        }
    }
}
