using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.DBContext.Migrations
{
  /// <inheritdoc />
  public partial class UPDATE_Scopes : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.Sql("UPDATE public.open_iddict_client_application SET permissions = '[\"ept:token\", \"gt:client_credentials\", \"scp:wekode.mml.identity.internal\" ]' WHERE permissions = '[\"ept:introspection\"]'");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {

    }
  }
}
