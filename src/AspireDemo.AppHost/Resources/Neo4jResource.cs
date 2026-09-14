using Aspire.Hosting.ApplicationModel;

namespace AspireDemo.AppHost.Resources;

public class Neo4jResource(string name, ParameterResource password) 
    : ContainerResource(name), IResourceWithConnectionString
{
    public const string DefaultRegistry = "docker.io";
    public const string DefaultImage = "neo4j";
    public const string DefaultTag = "2026.07.1-community-trixie";
    public const string BoltEndpointName = "neo4j";
    public const string AdminEndpointName = "adminUi";
    
    public EndpointReference BoltEndpoint => new(this, BoltEndpointName);

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"Endpoint={BoltEndpoint};Username=neo4j;Password={password}");
}