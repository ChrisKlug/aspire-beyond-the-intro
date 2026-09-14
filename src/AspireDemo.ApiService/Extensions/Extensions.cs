using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;

namespace AspireDemo.ApiService.Extensions;

public static class Extensions
{   
    public static WebApplicationBuilder AddNeo4jDriver(
        this WebApplicationBuilder builder, 
        string? connectionStringName = null
    )
    {
        if (connectionStringName is null)
        {
            return builder;
        }
        var connstring = new System.Data.Common.DbConnectionStringBuilder
        {
            ConnectionString = builder.Configuration.GetConnectionString(connectionStringName)!
        };
        builder.Services.AddSingleton<IDriver>(_ => GraphDatabase.Driver(
            (string)connstring["Endpoint"], 
            AuthTokens.Basic(
                (string)connstring["Username"], 
                (string)connstring["Password"]
            )
        ));
        return builder;
    }
    
    public static RouteHandlerBuilder MapUsersEndpoint(this WebApplication app)
        => app.MapGet("/api/users", async (IServiceProvider sp) =>
        {
            var driver = sp.GetService<IDriver>();
            if (driver is null)
            {
                return Array.Empty<string>();
            }

            var result = await driver.ExecutableQuery("MATCH (n:User) RETURN n.name").ExecuteAsync();
            return result.Result.Select(record => record["n.name"].As<string>()).ToArray();
        });

    public static void MapDatabaseSeedingEndpoint(this WebApplication app)
    {
        if (Environment.GetEnvironmentVariable("API_ADMIN_KEY") is not null)
        {
            app.MapPost("/admin/seeddatabase", async (
                [FromServices] IDriver driver,
                [FromHeader(Name = "X-AdminKey")] string? adminKey) =>
            {
                if (Environment.GetEnvironmentVariable("API_ADMIN_KEY") != adminKey)
                {
                    return Results.NotFound();
                }
                await driver.ExecutableQuery("MATCH (n) DETACH DELETE n").ExecuteAsync();
                await driver.ExecutableQuery("CREATE (:User {name:'Ray'})").ExecuteAsync();
                await driver.ExecutableQuery("CREATE (:User {name:'Linda'})").ExecuteAsync();
                await driver.ExecutableQuery("CREATE (:User {name:'Shay'})").ExecuteAsync();
                await driver.ExecutableQuery("CREATE (:User {name:'Sara'})").ExecuteAsync();

                return Results.Ok();
            });
        }
    }
}