namespace AspireDemo.Web.ApiClients;

public class UsersClient(HttpClient httpClient)
{
    public Task<string[]> GetUsersAsync(CancellationToken cancellationToken = default)
        => httpClient.GetFromJsonAsync<string[]>("api/users", cancellationToken)!;
}
