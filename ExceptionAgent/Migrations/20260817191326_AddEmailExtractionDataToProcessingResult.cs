using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExceptionAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailExtractionDataToProcessingResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AffectedQuantity",
                table: "EmailProcessingResults",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "EmailProcessingResults",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Evidence",
                table: "EmailProcessingResults",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "NewExpectedDate",
                table: "EmailProcessingResults",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AffectedQuantity",
                table: "EmailProcessingResults");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "EmailProcessingResults");

            migrationBuilder.DropColumn(
                name: "Evidence",
                table: "EmailProcessingResults");

            migrationBuilder.DropColumn(
                name: "NewExpectedDate",
                table: "EmailProcessingResults");
        }
    }
}
