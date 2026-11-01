using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RastreadorHabitos.Modulo.Habitos.Migrations
{
    /// <inheritdoc />
    public partial class InicialHabitos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Habitos");

            migrationBuilder.CreateTable(
                name: "Habitos",
                schema: "Habitos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Habitos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HabitosDias",
                schema: "Habitos",
                columns: table => new
                {
                    HabitoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabitosDias", x => new { x.HabitoId, x.DiaSemana });
                    table.CheckConstraint("CK_HabitosDias_DiaSemana", "[DiaSemana] BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_HabitosDias_Habitos_HabitoId",
                        column: x => x.HabitoId,
                        principalSchema: "Habitos",
                        principalTable: "Habitos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosDiarios",
                schema: "Habitos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HabitoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Cumplido = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosDiarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosDiarios_Habitos_HabitoId",
                        column: x => x.HabitoId,
                        principalSchema: "Habitos",
                        principalTable: "Habitos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Habitos_UsuarioId",
                schema: "Habitos",
                table: "Habitos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosDiarios_HabitoId_Fecha",
                schema: "Habitos",
                table: "RegistrosDiarios",
                columns: new[] { "HabitoId", "Fecha" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HabitosDias",
                schema: "Habitos");

            migrationBuilder.DropTable(
                name: "RegistrosDiarios",
                schema: "Habitos");

            migrationBuilder.DropTable(
                name: "Habitos",
                schema: "Habitos");
        }
    }
}
