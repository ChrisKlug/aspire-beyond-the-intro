using System.Text.RegularExpressions;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspireDemo.AppHost.Extensions;

internal partial class PenguinRoutingEventSubscriber(
    PenguinTunnelRegistry registry,
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService,
    IHostApplicationLifetime lifetime,
    ILogger<PenguinRoutingEventSubscriber> logger)
    : IDistributedApplicationEventingSubscriber
{
    [GeneratedRegex(@"https://[a-zA-Z0-9][a-zA-Z0-9.\-]*\.free\.pinggy\.net")]
    private static partial Regex PublicUrlPattern();

    public Task SubscribeAsync(
        IDistributedApplicationEventing eventing, 
        DistributedApplicationExecutionContext context, 
        CancellationToken cancellationToken
    )
    {
        // Intercept right after resources are built but before they fully start up
        eventing.Subscribe<AfterResourcesCreatedEvent>((@event, _) =>
        {
            foreach (var resource in @event.Model.Resources.OfType<PenguinRoutingResource>())
            {
                // Don't use the passed in cancellation token - it is canceled as soon as the event
                // handlers finish, which tears the watch down too soon
                var __ = Task.Run(() => WatchTunnelAsync(resource, lifetime.ApplicationStopping));
            }

            return Task.CompletedTask;
        });

        return Task.CompletedTask;
    }

    private async Task WatchTunnelAsync(PenguinRoutingResource resource, CancellationToken ct)
    {
        try
        {
            var resourceLogger = loggerService.GetLogger(resource);
            string? currentUrl = null;

            // WatchAsync replays the backlog and then streams, so we don't race the process' startup.
            // Note this is the *log* stream - the notification service only carries state snapshots.
            await foreach (var logLines in loggerService.WatchAsync(resource).WithCancellation(ct))
            {
                foreach (var line in logLines)
                {
                    var match = PublicUrlPattern().Match(line.Content);
                    if (!match.Success || match.Value == currentUrl)
                    {
                        continue;
                    }

                    currentUrl = match.Value;
                    registry.Publish(resource.Name, currentUrl);

                    resourceLogger.LogInformation("");
                    resourceLogger.LogInformation("🟢 Public proxy successfully established!");
                    resourceLogger.LogInformation("🔗 External Link: {Url}", currentUrl);

                    // Make it a clickable hyperlink on the dashboard grid. The State is left alone -
                    // the orchestrator owns that for executables, and the health check is what
                    // actually tells the dashboard the tunnel is ready.
                    await notificationService.PublishUpdateAsync(resource, state => state with
                    {
                        Urls = [new UrlSnapshot("public-endpoint", currentUrl, IsInternal: false)]
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
            // App is shutting down
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing streams for resource: {Name}", resource.Name);
        }
    }
}