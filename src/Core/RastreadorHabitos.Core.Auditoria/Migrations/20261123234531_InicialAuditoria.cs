using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RastreadorHabitos.Core.Auditoria.Migrations
{
    /// <inheritdoc />
    public partial class InicialAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Auditoria");

            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                schema: "Auditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Entidad = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EntidadId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValorAnterior = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValorNuevo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Entidad_FechaUtc",
                schema: "Auditoria",
                table: "RegistrosAuditoria",
                columns: new[] { "Entidad", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_FechaUtc",
                schema: "Auditoria",
                table: "RegistrosAuditoria",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_UsuarioId_FechaUtc",
                schema: "Auditoria",
                table: "RegistrosAuditoria",
                columns: new[] { "UsuarioId", "FechaUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosAuditoria",
                schema: "Auditoria");
        }
    }
}
