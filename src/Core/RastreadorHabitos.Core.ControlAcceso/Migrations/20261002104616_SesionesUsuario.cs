using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RastreadorHabitos.Core.ControlAcceso.Migrations
{
    /// <inheritdoc />
    public partial class SesionesUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SesionesUsuario",
                schema: "ControlAcceso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaInicioUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaExpiracionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCierreUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionesUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SesionesUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "ControlAcceso",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SesionesUsuario_UsuarioId_FechaCierreUtc",
                schema: "ControlAcceso",
                table: "SesionesUsuario",
                columns: new[] { "UsuarioId", "FechaCierreUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SesionesUsuario",
                schema: "ControlAcceso");
        }
    }
}
