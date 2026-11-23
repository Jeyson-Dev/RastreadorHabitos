using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RastreadorHabitos.Core.Notificaciones.Migrations
{
    /// <inheritdoc />
    public partial class ReintentosDeLaCola : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaUltimoIntentoUtc",
                schema: "Notificaciones",
                table: "CorreosEnCola",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Intentos",
                schema: "Notificaciones",
                table: "CorreosEnCola",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UltimoError",
                schema: "Notificaciones",
                table: "CorreosEnCola",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            // Los correos que ya salieron con la cola mínima salieron en un intento.
            migrationBuilder.Sql(
                "UPDATE [Notificaciones].[CorreosEnCola] SET [Intentos] = 1, [FechaUltimoIntentoUtc] = [FechaEnvioUtc] " +
                "WHERE [Estado] = N'Enviado';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaUltimoIntentoUtc",
                schema: "Notificaciones",
                table: "CorreosEnCola");

            migrationBuilder.DropColumn(
                name: "Intentos",
                schema: "Notificaciones",
                table: "CorreosEnCola");

            migrationBuilder.DropColumn(
                name: "UltimoError",
                schema: "Notificaciones",
                table: "CorreosEnCola");
        }
    }
}
