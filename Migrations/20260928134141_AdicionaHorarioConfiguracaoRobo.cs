using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisaoDeAguia.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaHorarioConfiguracaoRobo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "HorarioFim",
                table: "ConfiguracoesRobo",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HorarioInicio",
                table: "ConfiguracoesRobo",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HorarioFim",
                table: "ConfiguracoesRobo");

            migrationBuilder.DropColumn(
                name: "HorarioInicio",
                table: "ConfiguracoesRobo");
        }
    }
}
