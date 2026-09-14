using AspireDemo.AppHost.Extensions.Neo4j;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace AspireDemo.AppHost.Resources;

public static class Neo4jExtensions
{
    public static IResourceBuilder<Neo4jResource> AddNeo4j(
        this IDistributedApplicationBuilder builder, 
        string name,
        IResourceBuilder<ParameterResource>? password = null
    )
    {
        var passwordResource = password?.Resource
                               ?? builder.AddParameterFromConfiguration(
                                   $"{name}-password",
                                   $"Parameters:{name}:Password",
                                   secret: true)
                                   .WithCustomInput(parameter => new()
                                   {
                                       Name = parameter.Name,
                                       InputType = InputType.SecretText,
                                       Label = "Neo4j Password",
                                       Placeholder = "Enter a complex password",
                                       Description = "Password for the Neo4j user **neo4j**. Cannot be **neo4j**",
                                       EnableDescriptionMarkdown = true,
                                       Required = true
                                   })
                                   .WithHidden()
                                   .Resource;
        
        var neo4jResource = new Neo4jResource(name, passwordResource);

        return builder.AddResource(neo4jResource)
            .WithImage(Neo4jResource.DefaultImage)
            .WithImageRegistry(Neo4jResource.DefaultRegistry)
            .WithImageTag(Neo4jResource.DefaultTag)
            .WithEnvironment("NEO4J_AUTH", $"neo4j/{passwordResource}")
            .WithEndpoint(targetPort: 7687, port: 7687, name: Neo4jResource.BoltEndpointName, scheme: "neo4j")
            .WithEndpoint(targetPort: 7474, port: 7474, name: Neo4jResource.AdminEndpointName, scheme: "http")
            .WithUrlForEndpoint(Neo4jResource.AdminEndpointName, x =>
            {
                x.DisplayLocation = UrlDisplayLocation.SummaryAndDetails;
                x.DisplayText = "Admin UI";
            })
            .WithUrlForEndpoint(Neo4jResource.BoltEndpointName, x => { x.DisplayLocation = UrlDisplayLocation.DetailsOnly; });
    }

    public static IResourceBuilder<Neo4jResource> WithVolumeStorage(
        this IResourceBuilder<Neo4jResource> builder,
        string? name = null)
    {
        return builder.WithVolume(name, "/data");
    }

    public static IResourceBuilder<Neo4jResource> WithHttpsDeveloperCertificate(
        this IResourceBuilder<Neo4jResource> builder)
    {
        builder.WithHttpsCertificateConfiguration(async ctx =>
        {
            var certPath = await ctx.CertificatePath.GetValueAsync(ctx.CancellationToken);
            var keyPath = await ctx.KeyPath.GetValueAsync(ctx.CancellationToken);
            var basePath = Path.GetDirectoryName(certPath);

            ctx.EnvironmentVariables["NEO4J_server_https_enabled"] = "true";
            ctx.EnvironmentVariables["NEO4J_server_https_advertised__address"] = ":7473";
            ctx.EnvironmentVariables["NEO4J_dbms_ssl_policy_https_enabled"] = "true";
            ctx.EnvironmentVariables["NEO4J_dbms_ssl_policy_https_base__directory"] = basePath + "/";
            ctx.EnvironmentVariables["NEO4J_dbms_ssl_policy_https_public__certificate"] = Path.GetFileName(certPath)!;
            ctx.EnvironmentVariables["NEO4J_dbms_ssl_policy_https_private__key"] = Path.GetFileName(keyPath)!;
            ctx.EnvironmentVariables["NEO4J_dbms_ssl_policy_https_trust__all"] = "true";

            
        });

        var endpoint = builder.Resource.Annotations
            .OfType<EndpointAnnotation>()
            .First(e => e.Name == Neo4jResource.AdminEndpointName);
        builder.Resource.Annotations.Remove(endpoint);

        builder.WithEndpoint(
            targetPort: 7473,
            port: 7473,
            name: Neo4jResource.AdminEndpointName,
            scheme: "https");

        return builder;
    }

    public static IResourceBuilder<Neo4jResource> WithDatabaseSeedingCommand(
        this IResourceBuilder<Neo4jResource> builder)
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return builder;
        }
        
        builder.WithCommand(
            name: "seed-database",
            displayName: "Seed Database",
            executeCommand: async context =>
            {
                var seedDataService = context.Services.GetService<SeedDataService>();
                if (seedDataService is null)
                {
                    return CommandResults.Failure(
                        errorMessage: "Data seeding failed",
                        result: "**SeedDataService** is not registered in the service container.",
                        resultFormat: CommandResultFormat.Markdown);
                }

                var dataSource = context.Arguments.GetString("source")!;
                if (!Enum.TryParse<SeedDataSources>(dataSource, out var dataSourceType))
                {
                    return CommandResults.Failure("Data seeding failed", $"Invalid seed data source: {dataSource}");
                }

                var dataset = context.Arguments.GetString("dataset")!;

                var result = await seedDataService.SeedDatabaseAsync(dataSourceType, dataset);
                if (!result.Success)
                {
                    return CommandResults.Failure("Data seeding failed", result.ErrorMessage!);
                }

                return CommandResults.Success(
                    message: "Data seeding succeeded",
                    result: result.ToJson(),
                    resultFormat: CommandResultFormat.Json,
                    displayImmediately: true);
            },
            commandOptions: new CommandOptions
            {
                IconName = "ArchiveArrowBack",
                IconVariant = IconVariant.Filled,
                UpdateState = context =>
                    context.ResourceSnapshot.HealthStatus is HealthStatus
                        .Healthy
                        ? ResourceCommandState.Enabled
                        : ResourceCommandState.Disabled,
                Arguments =
                [
                    new InteractionInput
                    {
                        Name = "source",
                        Label = "Data Source",
                        InputType = InputType.Choice,
                        Options =
                        [
                            KeyValuePair.Create(nameof(SeedDataSources.Test), "Test Data"),
                            KeyValuePair.Create(nameof(SeedDataSources.Comics), "Comics Data")
                        ],
                        Required = true
                    },
                    new InteractionInput
                    {
                        Name = "dataset",
                        Label = "Dataset",
                        InputType = InputType.Text,
                        Required = true
                    },
                ],
                Visibility = ResourceCommandVisibility.Api
            }
        );

        return builder;
    }

    public static IResourceBuilder<Neo4jResource> WithInteractiveDatabaseSeedingCommand(
        this IResourceBuilder<Neo4jResource> builder)
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return builder;
        }
        
        builder.WithCommand(
            name: "seed-database-interactive",
            displayName: "Seed Database Interactively",
            executeCommand: async context =>
            {
                var seedDataService = context.Services.GetService<SeedDataService>();
                if (seedDataService is null)
                {
                    return CommandResults.Failure(
                        errorMessage: "Data seeding failed",
                        result: "**SeedDataService** is not registered in the service container.",
                        resultFormat: CommandResultFormat.Markdown);
                }

                var interactionService = context.Services.GetRequiredService<IInteractionService>();
                if (!interactionService.IsAvailable)
                {
                    return CommandResults.Failure(
                        errorMessage: "Data seeding failed",
                        result: "**IInteractionService** is not available.",
                        resultFormat: CommandResultFormat.Markdown);
                }

                var promptResult = await interactionService.PromptInputsAsync(
                    title: "Seed Configuration",
                    message: "Select the seed dataset to use",
                    inputs: new List<InteractionInput>
                    {
                        new()
                        {
                            Name = "source",
                            Label = "Data Source",
                            InputType = InputType.Choice,
                            Options =
                            [
                                KeyValuePair.Create(nameof(SeedDataSources.Test), "Test Data"),
                                KeyValuePair.Create(nameof(SeedDataSources.Comics), "Comics Data")
                            ],
                            Required = true
                        },
                        new()
                        {
                            Name = "dataset",
                            InputType = InputType.Choice,
                            Label = "Dataset",
                            Required = true,
                            DynamicLoading = new InputLoadOptions
                            {
                                LoadCallback = ctx =>
                                {
                                    var source = ctx.AllInputs["source"].Value;
                                    ctx.Input.Options =
                                        seedDataService.GetDatasets(Enum.Parse<SeedDataSources>(source!))
                                            .Select(dataset => KeyValuePair.Create(dataset, dataset))
                                            .ToList();
                                    return Task.CompletedTask;
                                },
                                DependsOnInputs = ["source"]
                            }
                        }
                    });

                if (promptResult.Canceled)
                {
                    return CommandResults.Canceled();
                }

                var dataSource = Enum.Parse<SeedDataSources>(promptResult.Data.GetString("source")!);
                var dataset = promptResult.Data.GetString("dataset")!;

                var seedConformation = await interactionService.PromptConfirmationAsync(
                    title: "Confirm Database Seeding",
                    message:
                    $"Are you sure you want to seed the database with **{dataSource}** - **{dataset}**? This action **cannot** be undone.",
                    options: new MessageBoxInteractionOptions
                    {
                        Intent = MessageIntent.Confirmation,
                        PrimaryButtonText = "Seed",
                        SecondaryButtonText = "Cancel",
                        ShowSecondaryButton = true,
                        EnableMessageMarkdown = true
                    });

                if (!seedConformation.Data)
                {
                    return CommandResults.Canceled();
                }

                SeedResult? seedResult = null;
                var result = await interactionService.PromptProgressAsync(
                    "Seeding database...",
                    new ProgressInteractionOptions
                    {
                        PrimaryButtonText = "Cancel",
                        Work = async progressContext =>
                        {
                            seedResult = await seedDataService.SeedDatabaseAsync(
                                dataSource,
                                dataset,
                                progressContext.CancellationToken
                            );
                        }
                    },
                    context.CancellationToken);

                if (result.Canceled)
                {
                    return CommandResults.Failure("Data seeding failed", "User cancelled seeding");
                }

                if (!seedResult!.Success)
                {
                    return CommandResults.Failure("Data seeding failed", seedResult.ErrorMessage!);
                }

                return CommandResults.Success(
                    message: "Data seeding succeeded",
                    result: seedResult.ToJson(),
                    resultFormat: CommandResultFormat.Json,
                    displayImmediately: true);
            },
            commandOptions: new CommandOptions
            {
                IconName = "ArchiveArrowBack",
                IconVariant = IconVariant.Filled,
                UpdateState = context =>
                    context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy
                        ? ResourceCommandState.Enabled
                        : ResourceCommandState.Disabled,
                Visibility = ResourceCommandVisibility.UI
            }
        );

        return builder;
    }

    public static IResourceBuilder<Neo4jResource> WithNeo4jIsAwesomeCommand(
        this IResourceBuilder<Neo4jResource> builder,
        string voice
    )
    {
        builder.WithProcessCommand(
            commandName: "say-neo4j-is-awesome-as" + voice,
            displayName: voice + " says what?",
            executablePath: "say",
            arguments: ["-v", voice, "Neo4j is awesome!"],
            commandOptions: new ProcessCommandOptions
            {
                IconName = "Speaker2",
                IconVariant = IconVariant.Filled,
                UpdateState = context =>
                    context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy
                        ? ResourceCommandState.Enabled
                        : ResourceCommandState.Disabled
            }
        );

        return builder;
    }
}