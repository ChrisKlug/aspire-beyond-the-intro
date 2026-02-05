using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;

namespace AspireDemo.Extensions.ConditionalActivation;

public class ConditionalActivationEventSubscriber(ConditionalResourcesActivationConfiguration config) : IDistributedApplicationEventingSubscriber
{
    public Task SubscribeAsync(
        IDistributedApplicationEventing eventing, 
        DistributedApplicationExecutionContext executionContext,
        CancellationToken cancellationToken
    )
    {
        eventing.Subscribe<BeforeStartEvent>(OnBeforeStartAsync);
        return Task.CompletedTask;
    }
    
    private Task OnBeforeStartAsync(BeforeStartEvent @event, CancellationToken cancellationToken = default)
    {
        if (config.CategoriesToActivate.Length == 0)
        {
            return Task.CompletedTask;
        }
        
        var resources = @event.Model.Resources.ToArray();
        
        var resourcesToActivate = resources
            .Where(ShouldBeActivated)
            .ToDictionary(x => x.Name, x => x);

        foreach (var resourceName in resourcesToActivate.Keys.ToArray())
        {
            var references = GetReferences(resourcesToActivate[resourceName]);
            foreach (var reference in references)
            {
                if (!resourcesToActivate.ContainsKey(reference.Resource.Name))
                {
                    var resource = resources.First(r => r.Name == reference.Resource.Name);
                    resourcesToActivate.Add(reference.Resource.Name, resource);
                }
            }
        }

        foreach (var resource in resources)
        {
            if (!resourcesToActivate.ContainsKey(resource.Name))
            {
                @event.Model.Resources.Remove(resource);
            }
        }
        
        return Task.CompletedTask;
    }

    private bool ShouldBeActivated(IResource resource)
    {
        if (resource.Name == "aspire-dashboard")
        {
            return true;
        }
        
        var annotations = resource.Annotations
            .OfType<ConditionalResourceActivationAnnotation>()
            .ToArray();
        
        return annotations.Length > 0 && 
               annotations.Any(x => config.CategoriesToActivate.Contains(x.Category));
    }
    private ResourceRelationshipAnnotation[] GetReferences(IResource resource)
    {
        return resource.Annotations
            .OfType<ResourceRelationshipAnnotation>()
            .Where(x => x.Type == "Reference")
            .ToArray();
    }
}