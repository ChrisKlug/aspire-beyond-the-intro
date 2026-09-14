namespace AspireDemo.Web.ApiClients;

public static class ApiClientExtensions
{
    public static IServiceCollection AddApiClient<T>(this IServiceCollection services) where T : class
    {
        services.AddHttpClient<T>(client =>
        {
            client.BaseAddress = new("https+http://apiservice/");
        });
        return services;
    }
}