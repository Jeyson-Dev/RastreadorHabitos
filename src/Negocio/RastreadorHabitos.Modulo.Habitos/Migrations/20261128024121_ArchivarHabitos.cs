using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RastreadorHabitos.Modulo.Habitos.Migrations
{
    /// <inheritdoc />
    public partial class ArchivarHabitos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaArchivadoUtc",
                schema: "Habitos",
                table: "Habitos",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaArchivadoUtc",
                schema: "Habitos",
                table: "Habitos");
        }
    }
}
