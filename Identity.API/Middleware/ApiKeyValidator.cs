using Identity.Application.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OpenIddict.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Identity.Middleware
{
  public class ApiKeyValidator(RequestDelegate next, IConfiguration configuration, IClientRepository clientRepository)
  {
    private const string APP_KEY_HEADER = "App-Key";
    private const string ADMIN_APP_KEY = "ADMIN_APP_KEY";
    private const string APP_KEY = "APP_KEY";

    public async Task Invoke(HttpContext context)
    {
      // allow request from api services to validate access token.
      if (context.Request.Headers.TryGetValue("ClientId", out Microsoft.Extensions.Primitives.StringValues clientId) && context.Request.Headers.TryGetValue("ClientSecret", out Microsoft.Extensions.Primitives.StringValues clientSecret))
      {
        if (clientRepository.IsApiClient(clientId!, clientSecret!))
        {
          await next.Invoke(context);
          return;
        }
      }

      var isAdminAppRequest = context.Request.Headers[APP_KEY_HEADER] == configuration.GetValue(ADMIN_APP_KEY, string.Empty);
      var isAppRequest = context.Request.Headers[APP_KEY_HEADER] == configuration.GetValue(APP_KEY, string.Empty);
      if (!isAdminAppRequest && !isAppRequest)
      {
        await UnauthorizedRespone(context);
        return;
      }

      await next.Invoke(context);
    }

    private static async Task UnauthorizedRespone(HttpContext context)
    {
      context.Response.StatusCode = 401; //Unauthorized               
      await context.Response.WriteAsync(string.Empty);
    }
  }

  public static class ApiKeyValidatorExtension
  {
    public static IApplicationBuilder UseApiKeyValidation(this IApplicationBuilder app)
    {
      app.UseMiddleware<ApiKeyValidator>();
      return app;
    }
  }
}
