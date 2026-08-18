using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExceptionAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailProcessingResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailProcessingResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmailId = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchingStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    PurchaseOrderId = table.Column<int>(type: "INTEGER", nullable: true),
                    MatchScore = table.Column<double>(type: "REAL", nullable: false),
                    MatchingReason = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailProcessingResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailProcessingResults_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailProcessingResults_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailProcessingResults_EmailId",
                table: "EmailProcessingResults",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailProcessingResults_PurchaseOrderId",
                table: "EmailProcessingResults",
                column: "PurchaseOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailProcessingResults");
        }
    }
}
