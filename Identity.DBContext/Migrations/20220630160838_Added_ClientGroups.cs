using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.DBContext.Migrations
{
    public partial class Added_ClientGroups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "group",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "group_open_iddict_client_application",
                schema: "public",
                columns: table => new
                {
                    clients_id = table.Column<string>(type: "text", nullable: false),
                    groups_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_open_iddict_client_application", x => new { x.clients_id, x.groups_id });
                    table.ForeignKey(
                        name: "fk_group_open_iddict_client_application_applications_clients_id",
                        column: x => x.clients_id,
                        principalSchema: "public",
                        principalTable: "open_iddict_client_application",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_open_iddict_client_application_groups_groups_id",
                        column: x => x.groups_id,
                        principalSchema: "public",
                        principalTable: "group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_group_open_iddict_client_application_groups_id",
                schema: "public",
                table: "group_open_iddict_client_application",
                column: "groups_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "group_open_iddict_client_application",
                schema: "public");

            migrationBuilder.DropTable(
                name: "group",
                schema: "public");
        }
    }
}
