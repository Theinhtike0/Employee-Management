using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_Products.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveBalanceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LEAV_BALANCE",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpeId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LEAV_BALANCE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LEAV_BALANCE_EMPE_PROFILE_EmpeId",
                        column: x => x.EmpeId,
                        principalTable: "EMPE_PROFILE",
                        principalColumn: "EmpeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LEAV_BALANCE_LEAV_TYPE_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LEAV_TYPE",
                        principalColumn: "LEAV_TYPE_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LEAV_BALANCE_EmpeId",
                table: "LEAV_BALANCE",
                column: "EmpeId");

            migrationBuilder.CreateIndex(
                name: "IX_LEAV_BALANCE_LeaveTypeId",
                table: "LEAV_BALANCE",
                column: "LeaveTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LEAV_BALANCE");
        }
    }
}
