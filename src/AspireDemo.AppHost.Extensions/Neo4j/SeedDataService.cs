using Neo4j.Driver;

namespace AspireDemo.AppHost.Extensions.Neo4j;

public class SeedDataService(IDriver driver)
{
    public async Task<SeedResult> SeedDatabaseAsync(
        SeedDataSources source, 
        string dataset,
        CancellationToken cancellationToken = default
    )
    {
        var users = DataSets.GetDataset(source, dataset);
        if (users is null)
        {
            return new SeedResult(source, dataset, null, $"Dataset '{dataset}' not found for source '{source}'.");
        }

        var session = driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync("MATCH (n) DETACH DELETE n");
                foreach (var user in users)
                {
                    await tx.RunAsync($"MERGE (n:User {{name: '{user}'}})");
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(1000, cancellationToken); // Simulate some processing time
                }
            }
        );
        return new SeedResult(source, dataset, users.Length, null);
    }
    
    public string[] GetDatasets(SeedDataSources source)
    {
        return DataSets.ForSource(source);
    }
}

public record SeedResult(
    SeedDataSources Source,
    string Dataset,
    int? SeedCount,
    string? ErrorMessage
)
{
    public string ToJson()
    {
        return $"{{\"seedCount\": {SeedCount}, \"source\": \"{Source}\", \"dataset\": \"{Dataset}\", \"errorMessage\": \"{ErrorMessage}\"}}";
    }
    public bool Success => ErrorMessage is null;
}

public enum SeedDataSources
{
    Test,
    Comics
}