using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kodx.Rpi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rpi_editions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    edicao = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    data_publicacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rpi_editions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "publications",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rpi_edition_id = table.Column<int>(type: "integer", nullable: false),
                    numero = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_publications", x => x.id);
                    table.ForeignKey(
                        name: "fk_publications_rpi_editions_rpi_edition_id",
                        column: x => x.rpi_edition_id,
                        principalTable: "rpi_editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rpi_processing_attempts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rpi_edition_id = table.Column<int>(type: "integer", nullable: false),
                    stage = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rpi_processing_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_rpi_processing_attempts_rpi_editions_rpi_edition_id",
                        column: x => x.rpi_edition_id,
                        principalTable: "rpi_editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_publications_numero",
                table: "publications",
                column: "numero");

            migrationBuilder.CreateIndex(
                name: "ix_publications_rpi_edition_id",
                table: "publications",
                column: "rpi_edition_id");

            migrationBuilder.CreateIndex(
                name: "ix_rpi_editions_edicao_tipo",
                table: "rpi_editions",
                columns: new[] { "edicao", "tipo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rpi_processing_attempts_rpi_edition_id_stage",
                table: "rpi_processing_attempts",
                columns: new[] { "rpi_edition_id", "stage" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "publications");

            migrationBuilder.DropTable(
                name: "rpi_processing_attempts");

            migrationBuilder.DropTable(
                name: "rpi_editions");
        }
    }
}
