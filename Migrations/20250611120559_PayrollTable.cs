using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_Products.Migrations
{
    /// <inheritdoc />
    public partial class PayrollTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PAYROLLS",
                table: "PAYROLLS");

            migrationBuilder.AlterColumn<int>(
                name: "EmpeId",
                table: "PAYROLLS",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "PayrollId",
                table: "PAYROLLS",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PAYROLLS",
                table: "PAYROLLS",
                column: "PayrollId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PAYROLLS",
                table: "PAYROLLS");

            migrationBuilder.DropColumn(
                name: "PayrollId",
                table: "PAYROLLS");

            migrationBuilder.AlterColumn<int>(
                name: "EmpeId",
                table: "PAYROLLS",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PAYROLLS",
                table: "PAYROLLS",
                column: "EmpeId");
        }
    }
}
