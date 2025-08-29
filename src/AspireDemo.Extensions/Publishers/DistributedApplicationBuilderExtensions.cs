using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using AspireDemo.AppHost.Publishers;

namespace AspireDemo.Extensions.Publishers;

public static class DistributedApplicationBuilderExtensions
{
    public static IResourceBuilder<EnterpriseEnvironmentResource> AddEnterpriseEnvironment(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        builder.Services.TryAddLifecycleHook<EnterpriseEnvironmentLifecycleHooks>();

        var resource = new EnterpriseEnvironmentResource(name);

        if (builder.ExecutionContext.IsRunMode)
        {
            // Return a builder that isn't added to the top-level application builder
            // so it doesn't surface as a resource.
            return builder.CreateResourceBuilder(resource);
        }

        return builder.AddResource(resource);
    }

    public static IResourceBuilder<EnterpriseEnvironmentResource> WithSender(
        this IResourceBuilder<EnterpriseEnvironmentResource> builder,
        string name)
    {
        return builder.WithAnnotation(new EnterpriseEnvironmentSenderAnnotation(name));
    }

    public static IResourceBuilder<EnterpriseEnvironmentResource> WithRecipient(
        this IResourceBuilder<EnterpriseEnvironmentResource> builder,
        string name,
        string email)
    {
        return builder.WithAnnotation(new EnterpriseEnvironmentRecipientAnnotation(name, email));
    }
    
}