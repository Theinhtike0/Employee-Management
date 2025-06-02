using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_Products.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LEAV_REQUESTS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeaveBalance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApproverName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LEAV_REQUESTS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LEAV_REQUESTS_EMPE_PROFILE_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "EMPE_PROFILE",
                        principalColumn: "EmpeId");
                    table.ForeignKey(
                        name: "FK_LEAV_REQUESTS_EMPE_PROFILE_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "EMPE_PROFILE",
                        principalColumn: "EmpeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LEAV_REQUESTS_LEAV_TYPE_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LEAV_TYPE",
                        principalColumn: "LEAV_TYPE_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LEAV_REQUESTS_ApprovedById",
                table: "LEAV_REQUESTS",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_LEAV_REQUESTS_EmployeeId",
                table: "LEAV_REQUESTS",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LEAV_REQUESTS_LeaveTypeId",
                table: "LEAV_REQUESTS",
                column: "LeaveTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LEAV_REQUESTS");
        }
    }
}
