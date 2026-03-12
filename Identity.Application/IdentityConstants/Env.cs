using System;

namespace Identity.Application.IdentityConstants;
public static class Env
{
  /// <summary>
  /// The instance of this service.
  /// </summary>
  public static string INSTANCE => Environment.GetEnvironmentVariable("INSTANCE") ?? throw new ArgumentNullException("INSTANCE", "Instance configuration is required");
}
