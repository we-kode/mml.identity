using System.Collections.Generic;

namespace Identity.Application.Models
{
  /// <summary>
  /// A list of clients
  /// </summary>
  public class Clients
  {
    /// <summary>
    /// The total count of available clients
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// On chunk of loaded clients.
    /// </summary>
    public IList<Client> Items { get; set; }

    /// <summary>
    /// Constructs new Instance
    /// </summary>
    public Clients()
    {
      Items = new List<Client>();
    }
  }
}
