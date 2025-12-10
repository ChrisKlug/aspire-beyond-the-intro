using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using AspireDemo.Extensions.Neo4j;
using AspireDemo.Extensions.Publishers;

namespace AspireDemo.Extensions;

public static class DistributedApplicationExtensions
{
    public static IResourceBuilder<Neo4jResource> AddNeo4j(
        this IDistributedApplicationBuilder builder, 
        string name,
        ParameterResource? password = null)
    {
        var neo4jResource = new Neo4jResource(name, password);

        return builder.AddResource(neo4jResource)
            .WithImage(Neo4jResource.DefaultImage)
            .WithImageRegistry(Neo4jResource.DefaultRegistry)
            .WithImageTag(Neo4jResource.DefaultTag)
            .WithEnvironment("NEO4J_AUTH", $"{Neo4jResource.Username}/{neo4jResource.PasswordParameter}")
            .WithEndpoint(targetPort: 7687, port: 7687, name: Neo4jResource.BoltEndpointName, scheme: "neo4j")
            .WithEndpoint(targetPort: 7474, port: 7474, name: Neo4jResource.AdminEndpointName, scheme: "http")
            .WithHttpHealthCheck("/", endpointName: Neo4jResource.AdminEndpointName)
            .WithUrlForEndpoint(Neo4jResource.AdminEndpointName, x =>
            {
                x.DisplayLocation = UrlDisplayLocation.SummaryAndDetails;
                x.DisplayText = "Admin UI";
            })
            .WithUrlForEndpoint(Neo4jResource.BoltEndpointName, x => { x.DisplayLocation = UrlDisplayLocation.DetailsOnly; });
    }

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