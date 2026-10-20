using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RastreadorHabitos.Core.GestionPermisos.Migrations
{
    /// <inheritdoc />
    public partial class InicialGestionPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "GestionPermisos");

            migrationBuilder.CreateTable(
                name: "SolicitudesPermiso",
                schema: "GestionPermisos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Permiso = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResueltaPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaResolucionUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesPermiso", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermiso_Estado",
                schema: "GestionPermisos",
                table: "SolicitudesPermiso",
                column: "Estado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesPermiso",
                schema: "GestionPermisos");
        }
    }
}
