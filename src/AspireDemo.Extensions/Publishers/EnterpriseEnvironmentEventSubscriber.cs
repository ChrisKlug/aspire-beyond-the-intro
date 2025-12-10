using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;

namespace AspireDemo.Extensions.Publishers;

public class EnterpriseEnvironmentEventSubscriber
    : IDistributedApplicationEventingSubscriber
{
    public Task SubscribeAsync(
        IDistributedApplicationEventing eventing,
        DistributedApplicationExecutionContext ctx,
        CancellationToken cancellationToken
    )
    {
        eventing.Subscribe<BeforeStartEvent>(OnBeforeStartAsync);
        return Task.CompletedTask;
    }

    private Task OnBeforeStartAsync(BeforeStartEvent @event, CancellationToken cancellationToken = default)
    {
        var environment = @event.Model.Resources.OfType<EnterpriseEnvironmentResource>().Single();

        var resources = @event.Model.GetComputeResources().ToList();
        resources.AddRange(@event.Model.Resources.OfType<ExternalServiceResource>());

        foreach (var r in resources)
        {
            EnterpriseServiceResource serviceResource = r switch
            {
                ProjectResource => new EnterpriseProjectResource(r.Name, r, environment),
                ContainerResource => new EnterpriseContainerResource(r.Name, r, environment),
                ExternalServiceResource => new EnterpriseExternalResource(r.Name, r, environment),
                _ => throw new Exception("Weird...")
            };

            r.Annotations.Add(new DeploymentTargetAnnotation(serviceResource)
            {
                ComputeEnvironment = environment
            });

            environment.ResourceMapping[r] = serviceResource;
        }
        return Task.CompletedTask;
    }
}