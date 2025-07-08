using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_Products.Migrations
{
    /// <inheritdoc />
    public partial class AddLastRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PENSION_EMPE_PROFILE_EmpeId",
                table: "PENSION");

            migrationBuilder.AlterColumn<int>(
                name: "Reason",
                table: "PENSION",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "PENSION",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "PENSION",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "Age",
                table: "PENSION",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "ApproverName",
                table: "PENSION",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "AttachFileContent",
                table: "PENSION",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachFileName",
                table: "PENSION",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachFilePath",
                table: "PENSION",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AttachFileSize",
                table: "PENSION",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachFileType",
                table: "PENSION",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AttachFileUploadDate",
                table: "PENSION",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmpeName",
                table: "PENSION",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PensionSalary",
                table: "PENSION",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceBonus",
                table: "PENSION",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ServiceYears",
                table: "PENSION",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "LEAV_BALANCE",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SERVICE_BONUS",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpeId = table.Column<int>(type: "int", nullable: false),
                    EmpeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServiceYears = table.Column<int>(type: "int", nullable: false),
                    BasicSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BonusAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastRequestDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApproverName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SERVICE_BONUS", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_SERVICE_BONUS_EMPE_PROFILE_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "EMPE_PROFILE",
                        principalColumn: "EmpeId");
                    table.ForeignKey(
                        name: "FK_SERVICE_BONUS_EMPE_PROFILE_EmpeId",
                        column: x => x.EmpeId,
                        principalTable: "EMPE_PROFILE",
                        principalColumn: "EmpeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SERVICE_BONUS_ApprovedById",
                table: "SERVICE_BONUS",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICE_BONUS_EmpeId",
                table: "SERVICE_BONUS",
                column: "EmpeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PENSION_EMPE_PROFILE_EmpeId",
                table: "PENSION",
                column: "EmpeId",
                principalTable: "EMPE_PROFILE",
                principalColumn: "EmpeId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PENSION_EMPE_PROFILE_EmpeId",
                table: "PENSION");

            migrationBuilder.DropTable(
                name: "SERVICE_BONUS");

            migrationBuilder.DropColumn(
                name: "ApproverName",
                table: "PENSION");

            migrationBuilder.DropColumn(
                name: "AttachFileContent",
                table: "PENSION");

            migrationBuilder.DropColumn(
                name: "AttachFileName",
                table: "PENSION");

            migrationBuilder.DropColumn(
                name: "AttachFilePath",
                table: "PENSION");

            migrationBuilder.DropColumn(
                name: "AttachFileSize",
                table: "PENSION");

            migrationBuilder.DropColumn(
                name: "AttachFileType",
                table: "PENSION");

            migrationBuilder.DropColumn(
                name: "AttachFileUploadDate",
                table: "PENSION");

            migrationBuilder.DropColumn(
                name: "EmpeName",
                table: "PENSION");

            migrationBuilder.DropColumn(
                name: "PensionSalary",
                table: "PENSION");

            migrationBuilder.DropColumn(
                name: "ServiceBonus",
                table: "PENSION");

            migrationBuilder.DropColumn(
                name: "ServiceYears",
                table: "PENSION");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "LEAV_BALANCE");

            migrationBuilder.AlterColumn<int>(
                name: "Reason",
                table: "PENSION",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "PENSION",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "PENSION",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "Age",
                table: "PENSION",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PENSION_EMPE_PROFILE_EmpeId",
                table: "PENSION",
                column: "EmpeId",
                principalTable: "EMPE_PROFILE",
                principalColumn: "EmpeId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
