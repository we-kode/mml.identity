using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Quartz;
using Quartz;
using System.Threading.Tasks;
using Xunit;

namespace IdentityService.Test;

public class QuartzSchedulerTests
{
  [Fact]
  public async Task Scheduler_RunsOpenIddictPruningJob()
  {
    await using var first = TestApplication.Build();
    await using var second = TestApplication.Build();
    first.CreateClient();
    second.CreateClient();

    foreach (var application in new[] { first, second })
    {
      var scheduler = await application.Services.GetRequiredService<ISchedulerFactory>().GetScheduler(TestContext.Current.CancellationToken);

      for (var i = 0; i < 50 && await scheduler.GetStatus(TestContext.Current.CancellationToken) != SchedulerStatus.Running; i++)
      {
        await Task.Delay(100, TestContext.Current.CancellationToken);
      }

      Assert.Equal(SchedulerStatus.Running, await scheduler.GetStatus(TestContext.Current.CancellationToken));
      Assert.True(await scheduler.Exists(OpenIddictQuartzJob.Identity, TestContext.Current.CancellationToken));
    }
  }
}
