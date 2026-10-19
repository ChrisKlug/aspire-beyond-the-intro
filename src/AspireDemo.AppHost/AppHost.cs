using Aspire.Hosting.DevTunnels;
using AspireDemo.AppHost.Extensions;
using AspireDemo.AppHost.Extensions.Neo4j;
using AspireDemo.AppHost.Resources;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddSeedDataService<Neo4jResource>("neo4j");

var neo4j = builder.AddNeo4j("neo4j")
                    .WithVolumeStorage("mydata")
                    .WithLifetime(ContainerLifetime.Persistent)
                    .WithHttpsDeveloperCertificate()
                    .WithDatabaseSeedingCommand()
                    .WithInteractiveDatabaseSeedingCommand()
                    .WithNeo4jIsAwesomeCommand(Voices.BadNews)
                    .WithNeo4jIsAwesomeCommand(Voices.Alva);

var chuckApi = builder.AddExternalService("chuckapi", "https://api.chucknorris.io");

var apiKey = Guid.NewGuid().ToString();

var apiService = builder.AddProject<Projects.Api>("apiservice")
        .WithEndpointsInEnvironment(x => x.TlsEnabled)
        .WithEnvironment("API_ADMIN_KEY", apiKey)
        .WithHttpHealthCheck("/health")
        .WithReference(chuckApi)
        .WithChildRelationship(chuckApi)
        .WithAnnotationDebugging()
        .WithReference(neo4j)
        .WaitFor(neo4j)
        .WithHttpCommand(path: "/admin/seeddatabase",
            displayName: "Seed Database",
            commandOptions: new HttpCommandOptions
            {
                IconName = "ArchiveArrowBack",
                IconVariant = IconVariant.Filled,
                ConfirmationMessage = "Are you sure you want to seed the database?",
                PrepareRequest = (context) =>
                {
                    context.Request.Headers.Add("X-AdminKey", apiKey);
                    return Task.CompletedTask;
                },
                UpdateState = context =>
                    context.ResourceSnapshot.HealthStatus is HealthStatus
                        .Healthy
                        ? ResourceCommandState.Enabled
                        : ResourceCommandState.Disabled
            });

var frontEnd = builder.AddProject<Projects.Web>("webfrontend", "httpsOnly")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpHealthCheck("/health")
    .WithPenguinRouting();

builder.AddDockerfile("games", "../../resources/games")
    .WithTerminal();

builder.AddDotnetProject("external", "../../external/External.Web/External.Web.csproj",
    options => { options.LaunchProfileName = "https"; });

var tunnel = builder
    .AddDevTunnel("devtunnel", null, new DevTunnelOptions { AllowAnonymous = true })
    .WithReference(frontEnd);

builder.Build().Run();
