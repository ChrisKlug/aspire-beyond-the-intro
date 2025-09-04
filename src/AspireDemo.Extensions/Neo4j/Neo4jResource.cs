using Aspire.Hosting.ApplicationModel;

namespace AspireDemo.Extensions.Neo4j;

public class Neo4jResource(string name,
    ParameterResource? username = null,
    ParameterResource? password = null) : ContainerResource(name), IResourceWithConnectionString
{
    public const string DefaultRegistry = "docker.io";
    public const string DefaultImage = "neo4j";
    public const string DefaultTag = "2025.07.1-community-bullseye";
    public const string BoltEndpointName = "neo4j";
    public const string AdminEndpointName = "http";

    private EndpointReference BoltEndpoint => new EndpointReference(this, BoltEndpointName);
    public ParameterResource UsernameParameter { get; }
        = username ?? new ParameterResource("neo4j-username", x => "neo4j");
    public ParameterResource PasswordParameter { get; }
        = password ?? new ParameterResource("neo4j-password", x => "P@ssw0rd123!", true);

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"Endpoint={BoltEndpoint};Username={UsernameParameter};Password={PasswordParameter}"
        );
}