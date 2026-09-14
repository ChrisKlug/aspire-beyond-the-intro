using System.Data.Common;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;

namespace AspireDemo.AppHost.Extensions.Neo4j;

public static class SeedDataServiceExtensions
{
    public static IDistributedApplicationBuilder AddSeedDataService<T>(
        this IDistributedApplicationBuilder builder, 
        string resourceName
    )
        where T: IResourceWithConnectionString
    {
        builder.Services.AddSingleton<IDriver>(_ =>
        {
            var resource = builder.Resources.OfType<T>().First(r => r.Name == resourceName);
            var connstring = new DbConnectionStringBuilder()
            {
                ConnectionString = resource
                    .ConnectionStringExpression
                    .GetValueAsync(CancellationToken.None)
                    .Result!
            };
            return GraphDatabase.Driver(
                (string)connstring["Endpoint"],
                AuthTokens.Basic(
                    (string)connstring["Username"],
                    (string)connstring["Password"]
                )
            );
        });
        builder.Services.AddTransient<SeedDataService>();
        
        return builder;
    }
}