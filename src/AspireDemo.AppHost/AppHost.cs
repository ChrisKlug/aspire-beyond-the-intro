using AspireDemo.Extensions;
using AspireDemo.Extensions.Neo4j;
using AspireDemo.Extensions.Publishers;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddEnterpriseEnvironment("demo")
    .WithRecipient("John", "john@theitdepartment.com")
    .WithSender("Chris");

var password = builder.AddParameter("neo4j-password", secret: true);

var neo4j = builder.AddNeo4j("neo4j", password.Resource)
    .WithVolumeStorage("mydata")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithSeedDatabaseCommand();

password.WithParentRelationship(neo4j);

var adminKey = builder.AddParameter("adminkey", secret: true)
    .WithCustomInput(x => new()
    {
        Name = "adminkey",
        Label = "Admin Key",
        InputType = InputType.Text,
        Placeholder = "Something complicated and secure",
        Required = true
    });

var chuckApi = builder.AddExternalService("chuckapi", "https://api.chucknorris.io")
    .WithHttpHealthCheck("/");

var apiService = builder.AddProject<Projects.AspireDemo_ApiService>("apiservice", "https")
    .WithHttpHealthCheck("/health")
    .WithEndpointsInEnvironment(x => x.UriScheme == "https")
    .WithEnvironment("ADMIN_KEY", adminKey)
    .WithHttpCommand(path: "/admin/seed",
        displayName: "Seed Database",
        commandOptions: new HttpCommandOptions
        {
            IconName = "ArchiveArrowBack",
            IconVariant = IconVariant.Filled,
            ConfirmationMessage = "Are you sure you want to seed the database?",
            PrepareRequest = async (context) =>
            {
                context.Request.Headers.Add(
                    "X-AdminKey",
                    $"{await adminKey.Resource.GetValueAsync(CancellationToken.None)}"
                );
            }
        })
    .WithReference(neo4j)
    .WaitFor(neo4j)
    .WithReference(chuckApi);

adminKey.WithParentRelationship(apiService);

builder.AddProject<Projects.AspireDemo_Web>("webfrontend", "https")
    .WithExternalHttpEndpoints()
    .WithEndpointsInEnvironment(x => x.UriScheme == "https")
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
