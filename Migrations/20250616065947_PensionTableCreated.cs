using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_Products.Migrations
{
    /// <inheritdoc />
    public partial class PensionTableCreated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "EMPE_PROFILE",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.CreateTable(
                name: "PENSION",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpeId = table.Column<int>(type: "int", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PENSION", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_PENSION_EMPE_PROFILE_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "EMPE_PROFILE",
                        principalColumn: "EmpeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PENSION_EMPE_PROFILE_EmpeId",
                        column: x => x.EmpeId,
                        principalTable: "EMPE_PROFILE",
                        principalColumn: "EmpeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PAYROLLS_EmpeId",
                table: "PAYROLLS",
                column: "EmpeId");

            migrationBuilder.CreateIndex(
                name: "IX_PENSION_ApprovedById",
                table: "PENSION",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_PENSION_EmpeId",
                table: "PENSION",
                column: "EmpeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PAYROLLS_EMPE_PROFILE_EmpeId",
                table: "PAYROLLS",
                column: "EmpeId",
                principalTable: "EMPE_PROFILE",
                principalColumn: "EmpeId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PAYROLLS_EMPE_PROFILE_EmpeId",
                table: "PAYROLLS");

            migrationBuilder.DropTable(
                name: "PENSION");

            migrationBuilder.DropIndex(
                name: "IX_PAYROLLS_EmpeId",
                table: "PAYROLLS");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "EMPE_PROFILE",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
