using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExceptionAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierEmailEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierEmailEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmailId = table.Column<int>(type: "INTEGER", nullable: false),
                    PurchaseOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", nullable: false),
                    NewExpectedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AffectedQuantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Evidence = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierEmailEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierEmailEvents_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierEmailEvents_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierEmailEvents_EmailId",
                table: "SupplierEmailEvents",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierEmailEvents_PurchaseOrderId",
                table: "SupplierEmailEvents",
                column: "PurchaseOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierEmailEvents");
        }
    }
}
