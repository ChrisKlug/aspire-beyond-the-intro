using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace AspireDemo.Extensions.Neo4j;

public static class DistributedApplicationBuilderExtensions
{
    public static IResourceBuilder<Neo4jResource> AddNeo4j(this IDistributedApplicationBuilder builder, 
        string name,
        ParameterResource? username = null,
        ParameterResource? password = null)
    {
        var neo4jResource = new Neo4jResource(name, username, password);
        return builder.AddResource(neo4jResource)
            .WithImage(Neo4jResource.DefaultImage)
            .WithImageRegistry(Neo4jResource.DefaultRegistry)
            .WithImageTag(Neo4jResource.DefaultTag)
            .WithEnvironment("NEO4J_AUTH", $"{neo4jResource.UsernameParameter}/{neo4jResource.PasswordParameter}")
            .WithEndpoint(targetPort: 7687, port: 7687, name: Neo4jResource.BoltEndpointName, scheme: "neo4j")
            .WithEndpoint(targetPort: 7474, port: 7474, name: Neo4jResource.AdminEndpointName, scheme: "http")
            .WithUrlForEndpoint(Neo4jResource.AdminEndpointName, x =>
            {
                x.DisplayLocation = UrlDisplayLocation.SummaryAndDetails;
                x.DisplayText = "Admin UI";
            })
            .WithUrlForEndpoint(Neo4jResource.BoltEndpointName, x => { x.DisplayLocation = UrlDisplayLocation.DetailsOnly; })
            .WithHttpHealthCheck("/", endpointName: Neo4jResource.AdminEndpointName);
    }
}