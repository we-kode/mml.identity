using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Identity.DBContext.Migrations
{
  public partial class Created_Init : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.EnsureSchema(
          name: "public");

      migrationBuilder.CreateTable(
          name: "identity_role",
          schema: "public",
          columns: table => new
          {
            id = table.Column<long>(type: "bigint", nullable: false)
                  .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
            normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
            concurrency_stamp = table.Column<string>(type: "text", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_identity_role", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "identity_user",
          schema: "public",
          columns: table => new
          {
            id = table.Column<long>(type: "bigint", nullable: false)
                  .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
            normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
            email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
            normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
            email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
            password_hash = table.Column<string>(type: "text", nullable: true),
            security_stamp = table.Column<string>(type: "text", nullable: true),
            concurrency_stamp = table.Column<string>(type: "text", nullable: true),
            phone_number = table.Column<string>(type: "text", nullable: true),
            phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
            two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
            lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
            access_failed_count = table.Column<int>(type: "integer", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_identity_user", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "identity_role_claim",
          schema: "public",
          columns: table => new
          {
            id = table.Column<int>(type: "integer", nullable: false)
                  .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            role_id = table.Column<long>(type: "bigint", nullable: false),
            claim_type = table.Column<string>(type: "text", nullable: true),
            claim_value = table.Column<string>(type: "text", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_identity_role_claim", x => x.id);
            table.ForeignKey(
                      name: "fk_identity_role_claim_identity_role_role_id",
                      column: x => x.role_id,
                      principalSchema: "public",
                      principalTable: "identity_role",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "identity_user_claim",
          schema: "public",
          columns: table => new
          {
            id = table.Column<int>(type: "integer", nullable: false)
                  .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            user_id = table.Column<long>(type: "bigint", nullable: false),
            claim_type = table.Column<string>(type: "text", nullable: true),
            claim_value = table.Column<string>(type: "text", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_identity_user_claim", x => x.id);
            table.ForeignKey(
                      name: "fk_identity_user_claim_identity_user_user_id",
                      column: x => x.user_id,
                      principalSchema: "public",
                      principalTable: "identity_user",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "identity_user_login",
          schema: "public",
          columns: table => new
          {
            login_provider = table.Column<string>(type: "text", nullable: false),
            provider_key = table.Column<string>(type: "text", nullable: false),
            provider_display_name = table.Column<string>(type: "text", nullable: true),
            user_id = table.Column<long>(type: "bigint", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_identity_user_login", x => new { x.login_provider, x.provider_key });
            table.ForeignKey(
                      name: "fk_identity_user_login_identity_user_user_id",
                      column: x => x.user_id,
                      principalSchema: "public",
                      principalTable: "identity_user",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "identity_user_role",
          schema: "public",
          columns: table => new
          {
            user_id = table.Column<long>(type: "bigint", nullable: false),
            role_id = table.Column<long>(type: "bigint", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_identity_user_role", x => new { x.user_id, x.role_id });
            table.ForeignKey(
                      name: "fk_identity_user_role_identity_role_role_id",
                      column: x => x.role_id,
                      principalSchema: "public",
                      principalTable: "identity_role",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "fk_identity_user_role_identity_user_user_id",
                      column: x => x.user_id,
                      principalSchema: "public",
                      principalTable: "identity_user",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "identity_user_token",
          schema: "public",
          columns: table => new
          {
            user_id = table.Column<long>(type: "bigint", nullable: false),
            login_provider = table.Column<string>(type: "text", nullable: false),
            name = table.Column<string>(type: "text", nullable: false),
            value = table.Column<string>(type: "text", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_identity_user_token", x => new { x.user_id, x.login_provider, x.name });
            table.ForeignKey(
                      name: "fk_identity_user_token_identity_user_user_id",
                      column: x => x.user_id,
                      principalSchema: "public",
                      principalTable: "identity_user",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "RoleNameIndex",
          schema: "public",
          table: "identity_role",
          column: "normalized_name",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_identity_role_claim_role_id",
          schema: "public",
          table: "identity_role_claim",
          column: "role_id");

      migrationBuilder.CreateIndex(
          name: "EmailIndex",
          schema: "public",
          table: "identity_user",
          column: "normalized_email");

      migrationBuilder.CreateIndex(
          name: "UserNameIndex",
          schema: "public",
          table: "identity_user",
          column: "normalized_user_name",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_identity_user_claim_user_id",
          schema: "public",
          table: "identity_user_claim",
          column: "user_id");

      migrationBuilder.CreateIndex(
          name: "ix_identity_user_login_user_id",
          schema: "public",
          table: "identity_user_login",
          column: "user_id");

      migrationBuilder.CreateIndex(
          name: "ix_identity_user_role_role_id",
          schema: "public",
          table: "identity_user_role",
          column: "role_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "identity_role_claim",
          schema: "public");

      migrationBuilder.DropTable(
          name: "identity_user_claim",
          schema: "public");

      migrationBuilder.DropTable(
          name: "identity_user_login",
          schema: "public");

      migrationBuilder.DropTable(
          name: "identity_user_role",
          schema: "public");

      migrationBuilder.DropTable(
          name: "identity_user_token",
          schema: "public");

      migrationBuilder.DropTable(
          name: "identity_role",
          schema: "public");

      migrationBuilder.DropTable(
          name: "identity_user",
          schema: "public");
    }
  }
}
