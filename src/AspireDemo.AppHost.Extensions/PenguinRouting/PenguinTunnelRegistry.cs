using System.Collections.Concurrent;

namespace AspireDemo.AppHost.Extensions;

internal sealed class PenguinTunnelRegistry
{
    private readonly ConcurrentDictionary<string, string> _urls = new();

    public void Publish(string resourceName, string url) => _urls[resourceName] = url;

    public bool TryGetUrl(string resourceName, out string? url) => _urls.TryGetValue(resourceName, out url);
}