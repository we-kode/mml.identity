using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.DBContext.Migrations
{
    /// <inheritdoc />
    public partial class Updated_DotNet8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "type",
                schema: "public",
                table: "open_iddict_client_application",
                newName: "client_type");

            migrationBuilder.AddColumn<string>(
                name: "application_type",
                schema: "public",
                table: "open_iddict_client_application",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "json_web_key_set",
                schema: "public",
                table: "open_iddict_client_application",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "settings",
                schema: "public",
                table: "open_iddict_client_application",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "application_type",
                schema: "public",
                table: "open_iddict_client_application");

            migrationBuilder.DropColumn(
                name: "json_web_key_set",
                schema: "public",
                table: "open_iddict_client_application");

            migrationBuilder.DropColumn(
                name: "settings",
                schema: "public",
                table: "open_iddict_client_application");

            migrationBuilder.RenameColumn(
                name: "client_type",
                schema: "public",
                table: "open_iddict_client_application",
                newName: "type");
        }
    }
}
