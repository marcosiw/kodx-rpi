using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kodx.Rpi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicationNumeros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "publication_numeros",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    publication_id = table.Column<int>(type: "integer", nullable: false),
                    numero = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_publication_numeros", x => x.id);
                    table.ForeignKey(
                        name: "fk_publication_numeros_publications_publication_id",
                        column: x => x.publication_id,
                        principalTable: "publications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_publication_numeros_numero",
                table: "publication_numeros",
                column: "numero");

            migrationBuilder.CreateIndex(
                name: "ix_publication_numeros_publication_id",
                table: "publication_numeros",
                column: "publication_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "publication_numeros");
        }
    }
}
