using Identity.Application.Utils;
using RabbitMQ.Client;
using System;
using Xunit;

namespace IdentityService.Test
{
  public class MessageBusConnectionTests
  {
    [Theory]
    [InlineData("guest", "guest")]
    [InlineData("mml@user", "p@ss:w/rd#?%;,+ ")]
    public void Create_RoundTripsCredentials(string user, string password)
    {
      var factory = new ConnectionFactory { Uri = new Uri(MessageBusConnection.Create("wekode.mml.mbus", user, password, "/")) };

      Assert.Equal("wekode.mml.mbus", factory.HostName);
      Assert.Equal(user, factory.UserName);
      Assert.Equal(password, factory.Password);
      Assert.Equal("/", factory.VirtualHost);
    }

    [Fact]
    public void Create_KeepsPortAndDefaultVirtualHost()
    {
      var factory = new ConnectionFactory { Uri = new Uri(MessageBusConnection.Create("wekode.mml.mbus:5673", "guest", "guest", null)) };

      Assert.Equal(5673, factory.Port);
      Assert.Equal("/", factory.VirtualHost);
    }
  }
}
