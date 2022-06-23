using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.DBContext.Migrations
{
    public partial class Added_TokenRefreshDate_DeviceName_Clients : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "device_name",
                schema: "public",
                table: "open_iddict_client_application",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_token_refresh_date",
                schema: "public",
                table: "open_iddict_client_application",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "device_name",
                schema: "public",
                table: "open_iddict_client_application");

            migrationBuilder.DropColumn(
                name: "last_token_refresh_date",
                schema: "public",
                table: "open_iddict_client_application");
        }
    }
}
