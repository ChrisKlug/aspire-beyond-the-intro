namespace AspireDemo.Web.ApiClients;

public class ChuckQuotesClient(HttpClient httpClient)
{
    public Task<string> GetQuoteAsync(CancellationToken cancellationToken = default)
        => httpClient.GetStringAsync("/api/chuck-quote", cancellationToken)!;
}
