using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.DBContext.Migrations
{
    public partial class Changed_ColumnName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "device_name",
                schema: "public",
                table: "open_iddict_client_application",
                newName: "device_identifier");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "device_identifier",
                schema: "public",
                table: "open_iddict_client_application",
                newName: "device_name");
        }
    }
}
