using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.DBContext.Migrations
{
    public partial class Added_ClientGroup : Migration
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
                name: "client_group",
                schema: "public",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "text", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_group", x => new { x.client_id, x.group_id });
                    table.ForeignKey(
                        name: "fk_client_group_groups_group_id",
                        column: x => x.group_id,
                        principalSchema: "public",
                        principalTable: "group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_client_group_open_iddict_applications_client_id",
                        column: x => x.client_id,
                        principalSchema: "public",
                        principalTable: "open_iddict_client_application",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_client_group_group_id",
                schema: "public",
                table: "client_group",
                column: "group_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_group",
                schema: "public");

            migrationBuilder.DropTable(
                name: "group",
                schema: "public");
        }
    }
}
