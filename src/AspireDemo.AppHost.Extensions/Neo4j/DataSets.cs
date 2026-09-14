namespace AspireDemo.AppHost.Extensions.Neo4j;

public static class DataSets
{
    private static readonly Dictionary<SeedDataSources, Dictionary<string, string[]>[]> TestSources = new()
    {
        [SeedDataSources.Test] =
        [
            new Dictionary<string, string[]>
            {
                ["dataset1"] = ["Anna", "John"],
                ["dataset2"] = ["Erik", "Siobhan"]
            }
        ],
        [SeedDataSources.Comics] =
        [
            new Dictionary<string, string[]>
            {
                ["Batman"] = ["Bruce", "Selina"],
                ["Spiderman"] = ["Peter", "Mary Jane"]
            }
        ]
    };
    
    public static string[]? GetDataset(SeedDataSources source, string datasetName)
    {
        if (TestSources.TryGetValue(source, out var datasets))
        {
            foreach (var dataset in datasets)
            {
                if (dataset.TryGetValue(datasetName, out var data))
                {
                    return data;
                }
            }
        }
        return null;
    }

    public static string[] ForSource(SeedDataSources source)
        => TestSources[source].SelectMany(d => d.Keys).ToArray();
}