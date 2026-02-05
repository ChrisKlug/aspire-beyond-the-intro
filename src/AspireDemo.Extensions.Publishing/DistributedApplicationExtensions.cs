using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace AspireDemo.Extensions.Publishing;

public static class DistributedApplicationExtensions
{
    public static IResourceBuilder<EnterpriseEnvironmentResource> AddEnterpriseEnvironment(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var resource = new EnterpriseEnvironmentResource(name);

        if (builder.ExecutionContext.IsRunMode)
        {
            // Return a builder that isn't added to the top-level application builder
            // so it doesn't surface as a resource.
            return builder.CreateResourceBuilder(resource);
        }

        builder.Services.TryAddEventingSubscriber<EnterpriseEnvironmentEventSubscriber>();

        return builder.AddResource(resource);
    }
}