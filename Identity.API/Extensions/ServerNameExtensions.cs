using Microsoft.Extensions.Configuration;
using System;

namespace Identity.Extensions
{
  /// <summary>
  /// Provides the server name registered clients should connect to.
  /// </summary>
  public static class ServerNameExtensions
  {
    /// <summary>
    /// Configuration key of the server name in the format host[:port].
    /// </summary>
    public const string SERVER_NAME = "Connection:ServerName";

    /// <summary>
    /// Returns the configured server name in the format host[:port] or null, if none is configured.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the configured value is not a host with an optional port.</exception>
    public static string? GetServerName(this IConfiguration configuration)
    {
      var serverName = configuration[SERVER_NAME]?.Trim();
      if (string.IsNullOrEmpty(serverName))
      {
        return null;
      }

      if (!Uri.TryCreate($"https://{serverName}", UriKind.Absolute, out var uri)
        || !string.Equals(uri.Authority, serverName, StringComparison.OrdinalIgnoreCase)
        || uri.PathAndQuery != "/"
        || !string.IsNullOrEmpty(uri.Fragment))
      {
        throw new InvalidOperationException($"'{SERVER_NAME}' must be a host with an optional port without scheme, path or default port 443, e.g. 'media.example.com' or 'media.example.com:5050'.");
      }

      return serverName;
    }
  }
}
