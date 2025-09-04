using System.Data.Common;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Neo4j.Driver;

namespace AspireDemo.Extensions.Neo4j;

public static class Neo4jResourceExtensions
{
    public static IResourceBuilder<Neo4jResource> WithVolumeStorage(
        this IResourceBuilder<Neo4jResource> builder,
        string? name = null)
    {
        return builder.WithVolume(name, "/data");
    }

    public static IResourceBuilder<Neo4jResource> WithSeedDatabaseCommand(
        this IResourceBuilder<Neo4jResource> builder)
    {
        return builder.WithCommand(
            name: "seed",
            displayName: "Seed Database",
            commandOptions: new()
            {
                UpdateState = context => context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy
                    ? ResourceCommandState.Enabled
                    : ResourceCommandState.Disabled,
                IconName = "ArchiveArrowBack",
                IconVariant = IconVariant.Filled,
                ConfirmationMessage = "Are you sure you want to seed the database?"
            },
            executeCommand: async context =>
            {
                var connstring = new DbConnectionStringBuilder
                {
                    ConnectionString =
                        await builder.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
                };

                await using var driver = GraphDatabase.Driver(
                    (string)connstring["Endpoint"],
                    AuthTokens.Basic(
                        (string)connstring["Username"],
                        (string)connstring["Password"]
                    )
                );

                await driver.ExecutableQuery("MATCH (n) DETACH DELETE n").ExecuteAsync();
                await driver.ExecutableQuery("CREATE (:User {name:'Chris'})").ExecuteAsync();
                await driver.ExecutableQuery("CREATE (:User {name:'Erica'})").ExecuteAsync();
                await driver.ExecutableQuery("CREATE (:User {name:'John'})").ExecuteAsync();
                await driver.ExecutableQuery("CREATE (:User {name:'Lisa'})").ExecuteAsync();

                var interactionService = context.ServiceProvider.GetRequiredService<IInteractionService>();
                if (interactionService.IsAvailable)
                {
                    _ = interactionService.PromptNotificationAsync(
                        title: "Database Seed Complete",
                        message: "Database has not been seeded with 4 users…",
                        options: new NotificationInteractionOptions
                        {
                            Intent = MessageIntent.Information
                        });
                }
                
                return new ExecuteCommandResult { Success = true };
            }
        );
    }
}