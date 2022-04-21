using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.DBContext.Migrations
{
  public partial class Added_Openiddict : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "open_iddict_entity_framework_core_application",
          schema: "public",
          columns: table => new
          {
            id = table.Column<string>(type: "text", nullable: false),
            client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            client_secret = table.Column<string>(type: "text", nullable: true),
            concurrency_token = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            consent_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            display_name = table.Column<string>(type: "text", nullable: true),
            display_names = table.Column<string>(type: "text", nullable: true),
            permissions = table.Column<string>(type: "text", nullable: true),
            post_logout_redirect_uris = table.Column<string>(type: "text", nullable: true),
            properties = table.Column<string>(type: "text", nullable: true),
            redirect_uris = table.Column<string>(type: "text", nullable: true),
            requirements = table.Column<string>(type: "text", nullable: true),
            type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_open_iddict_entity_framework_core_application", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "open_iddict_entity_framework_core_scope",
          schema: "public",
          columns: table => new
          {
            id = table.Column<string>(type: "text", nullable: false),
            concurrency_token = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            description = table.Column<string>(type: "text", nullable: true),
            descriptions = table.Column<string>(type: "text", nullable: true),
            display_name = table.Column<string>(type: "text", nullable: true),
            display_names = table.Column<string>(type: "text", nullable: true),
            name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
            properties = table.Column<string>(type: "text", nullable: true),
            resources = table.Column<string>(type: "text", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_open_iddict_entity_framework_core_scope", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "open_iddict_entity_framework_core_authorization",
          schema: "public",
          columns: table => new
          {
            id = table.Column<string>(type: "text", nullable: false),
            application_id = table.Column<string>(type: "text", nullable: true),
            concurrency_token = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            creation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            properties = table.Column<string>(type: "text", nullable: true),
            scopes = table.Column<string>(type: "text", nullable: true),
            status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            subject = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
            type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_open_iddict_entity_framework_core_authorization", x => x.id);
            table.ForeignKey(
                      name: "fk_open_iddict_entity_framework_core_authorization_open_iddict",
                      column: x => x.application_id,
                      principalSchema: "public",
                      principalTable: "open_iddict_entity_framework_core_application",
                      principalColumn: "id");
          });

      migrationBuilder.CreateTable(
          name: "open_iddict_entity_framework_core_token",
          schema: "public",
          columns: table => new
          {
            id = table.Column<string>(type: "text", nullable: false),
            application_id = table.Column<string>(type: "text", nullable: true),
            authorization_id = table.Column<string>(type: "text", nullable: true),
            concurrency_token = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            creation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            payload = table.Column<string>(type: "text", nullable: true),
            properties = table.Column<string>(type: "text", nullable: true),
            redemption_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            reference_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            subject = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
            type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_open_iddict_entity_framework_core_token", x => x.id);
            table.ForeignKey(
                      name: "fk_open_iddict_entity_framework_core_token_open_iddict_entity_",
                      column: x => x.application_id,
                      principalSchema: "public",
                      principalTable: "open_iddict_entity_framework_core_application",
                      principalColumn: "id");
            table.ForeignKey(
                      name: "fk_open_iddict_entity_framework_core_token_open_iddict_entity_1",
                      column: x => x.authorization_id,
                      principalSchema: "public",
                      principalTable: "open_iddict_entity_framework_core_authorization",
                      principalColumn: "id");
          });

      migrationBuilder.CreateIndex(
          name: "ix_open_iddict_entity_framework_core_application_client_id",
          schema: "public",
          table: "open_iddict_entity_framework_core_application",
          column: "client_id",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_open_iddict_entity_framework_core_authorization_application",
          schema: "public",
          table: "open_iddict_entity_framework_core_authorization",
          columns: new[] { "application_id", "status", "subject", "type" });

      migrationBuilder.CreateIndex(
          name: "ix_open_iddict_entity_framework_core_scope_name",
          schema: "public",
          table: "open_iddict_entity_framework_core_scope",
          column: "name",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_open_iddict_entity_framework_core_token_application_id_stat",
          schema: "public",
          table: "open_iddict_entity_framework_core_token",
          columns: new[] { "application_id", "status", "subject", "type" });

      migrationBuilder.CreateIndex(
          name: "ix_open_iddict_entity_framework_core_token_authorization_id",
          schema: "public",
          table: "open_iddict_entity_framework_core_token",
          column: "authorization_id");

      migrationBuilder.CreateIndex(
          name: "ix_open_iddict_entity_framework_core_token_reference_id",
          schema: "public",
          table: "open_iddict_entity_framework_core_token",
          column: "reference_id",
          unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "open_iddict_entity_framework_core_scope",
          schema: "public");

      migrationBuilder.DropTable(
          name: "open_iddict_entity_framework_core_token",
          schema: "public");

      migrationBuilder.DropTable(
          name: "open_iddict_entity_framework_core_authorization",
          schema: "public");

      migrationBuilder.DropTable(
          name: "open_iddict_entity_framework_core_application",
          schema: "public");
    }
  }
}
