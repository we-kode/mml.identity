using Identity.Application.Contracts;
using Identity.DBContext;
using Identity.DBContext.Models;
using Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rebus.Bus;
using System;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IdentityService.Test;

public class SqlClientRepositoryTests
{
  private const string Secret = "legacy-client-secret";

  [Theory]
  [InlineData("AQAAAAEACSfAAAAAEL20p4YFo//dcgxzwTXYVaWTgPs+VQqCcR8lxtLZPq38DTbYKHZ/CPt34NncQJu8gw==")] // CryptoHelper 4.0.0
  [InlineData("AQAAAAEAACcQAAAAEFdBCcrJgM1IskzVRE+F7+8iCqnM7fPKfZZtGOZ7VCG/N02nCANw2Mf2Hee7UUbtIQ==")] // CryptoHelper 3.0.2
  public void IsApiClient_VerifiesSecretHashedByPreviousVersions(string storedHash)
  {
    var repository = CreateRepository(storedHash);

    Assert.True(repository.IsApiClient("media", Secret));
    Assert.False(repository.IsApiClient("media", "wrong-secret"));
  }

  [Fact]
  public void IsApiClient_RejectsUnknownClient()
  {
    var repository = CreateRepository("AQAAAAEACSfAAAAAEL20p4YFo//dcgxzwTXYVaWTgPs+VQqCcR8lxtLZPq38DTbYKHZ/CPt34NncQJu8gw==");

    Assert.False(repository.IsApiClient("unknown", Secret));
  }

  private static SqlClientRepository CreateRepository(string storedHash)
  {
    var options = new DbContextOptionsBuilder<ApplicationDBContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    using (var context = new ApplicationDBContext(options))
    {
      context.Applications.Add(new OpenIddictClientApplication { Id = "1", ClientId = "media", ClientSecret = storedHash, ClientType = ClientTypes.Confidential });
      context.SaveChanges();
    }

    return new SqlClientRepository(() => new ApplicationDBContext(options), Mock.Of<IGroupRepository>(), Mock.Of<IBus>());
  }
}
