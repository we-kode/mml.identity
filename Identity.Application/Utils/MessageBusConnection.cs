using System;

namespace Identity.Application.Utils;

/// <summary>
/// Builds the connection string of the message bus.
/// </summary>
public static class MessageBusConnection
{
  /// <summary>
  /// Creates the AMQP connection string. User, password and virtual host are escaped, so they may contain reserved URI characters.
  /// </summary>
  /// <param name="host">Host of the message bus, optionally with port.</param>
  /// <param name="user">User of the message bus.</param>
  /// <param name="password">Password of the message bus.</param>
  /// <param name="virtualHost">Optional virtual host.</param>
  /// <returns>The AMQP connection string.</returns>
  public static string Create(string host, string user, string password, string? virtualHost)
  {
    var connection = $"amqp://{Uri.EscapeDataString(user)}:{Uri.EscapeDataString(password)}@{host}";
    return string.IsNullOrEmpty(virtualHost) ? connection : $"{connection}/{Uri.EscapeDataString(virtualHost)}";
  }
}
