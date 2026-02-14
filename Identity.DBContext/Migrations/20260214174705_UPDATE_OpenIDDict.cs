using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.DBContext.Migrations
{
    /// <inheritdoc />
    public partial class UPDATE_OpenIDDict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "public",
                table: "open_iddict_client_token",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "identity_passkey_data",
                schema: "public",
                columns: table => new
                {
                    public_key = table.Column<byte[]>(type: "bytea", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sign_count = table.Column<long>(type: "bigint", nullable: false),
                    transports = table.Column<string[]>(type: "text[]", nullable: true),
                    is_user_verified = table.Column<bool>(type: "boolean", nullable: false),
                    is_backup_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    is_backed_up = table.Column<bool>(type: "boolean", nullable: false),
                    attestation_object = table.Column<byte[]>(type: "bytea", nullable: false),
                    client_data_json = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "identity_passkey_data",
                schema: "public");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "public",
                table: "open_iddict_client_token",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);
        }
    }
}
