using Identity.DBContext.Extensions;
using Identity.DBContext.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.DBContext
{
  public class ApplicationDBContext : IdentityDbContext<IdentityUser<long>, IdentityRole<long>, long>
  {
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
    {
    }

    public DbSet<OpenIddictClientApplication> Applications => Set<OpenIddictClientApplication>();

    public DbSet<OpenIddictClientAuthorization> Authorizations => Set<OpenIddictClientAuthorization>();

    public DbSet<OpenIddictClientToken> Tokens => Set<OpenIddictClientToken>();

    public DbSet<Group> Groups => Set<Group>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSnakeCaseNamingConvention();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.HasDefaultSchema("public");
      base.OnModelCreating(modelBuilder);
      modelBuilder.UseOpenIddict<OpenIddictClientApplication, OpenIddictClientAuthorization, OpenIddictClientScope, OpenIddictClientToken, string>();

      foreach (var entity in modelBuilder.Model.GetEntityTypes())
      {
        var currentTableName = modelBuilder.Entity(entity.Name).Metadata.GetDefaultTableName();
        if (currentTableName!.Contains('<'))
        {
          currentTableName = currentTableName.Split('<')[0];
        }
        modelBuilder.Entity(entity.Name).ToTable(currentTableName.ToUnderscoreCase());
      }
    }
  }
}
