using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExceptionAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailMatchCandidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailMatchCandidate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmailProcessingResultId = table.Column<int>(type: "INTEGER", nullable: false),
                    PurchaseOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Score = table.Column<double>(type: "REAL", nullable: false),
                    Reasons = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMatchCandidate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailMatchCandidate_EmailProcessingResults_EmailProcessingResultId",
                        column: x => x.EmailProcessingResultId,
                        principalTable: "EmailProcessingResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailMatchCandidate_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailMatchCandidate_EmailProcessingResultId",
                table: "EmailMatchCandidate",
                column: "EmailProcessingResultId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMatchCandidate_PurchaseOrderId",
                table: "EmailMatchCandidate",
                column: "PurchaseOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailMatchCandidate");
        }
    }
}
