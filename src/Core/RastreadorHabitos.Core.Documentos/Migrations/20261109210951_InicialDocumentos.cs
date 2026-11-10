using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RastreadorHabitos.Core.Documentos.Migrations
{
    /// <inheritdoc />
    public partial class InicialDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Documentos");

            migrationBuilder.CreateTable(
                name: "Documentos",
                schema: "Documentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    FechaSubidaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubidoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RutaAlmacenamiento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacionUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_SubidoPorId_Eliminado",
                schema: "Documentos",
                table: "Documentos",
                columns: new[] { "SubidoPorId", "Eliminado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documentos",
                schema: "Documentos");
        }
    }
}
