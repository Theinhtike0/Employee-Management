using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_Products.Migrations
{
    /// <inheritdoc />
    public partial class cls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LEAVE_TYPE_NAME",
                table: "LEAV_SCHEME_TYPE_DETL",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LEAVE_TYPE_NAME",
                table: "LEAV_SCHEME_TYPE_DETL");
        }
    }
}
