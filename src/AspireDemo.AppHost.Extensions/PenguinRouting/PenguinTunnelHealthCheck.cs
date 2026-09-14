using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AspireDemo.AppHost.Extensions;

internal sealed class PenguinTunnelHealthCheck(PenguinTunnelRegistry registry, string resourceName) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        => Task.FromResult(registry.TryGetUrl(resourceName, out var url)
            ? HealthCheckResult.Healthy($"Tunnel established: {url}")
            : HealthCheckResult.Unhealthy("Waiting for Pinggy to hand out a public URL"));
}