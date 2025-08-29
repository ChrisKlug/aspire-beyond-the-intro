using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries =
    ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

IDriver GetDriver()
{
    var connstring = new DbConnectionStringBuilder
    {
        ConnectionString = builder.Configuration.GetConnectionString("neo4j")!
    };
    return GraphDatabase.Driver(
        (string)connstring["Endpoint"],
        AuthTokens.Basic(
            (string)connstring["Username"],
            (string)connstring["Password"]
        )
    );
}

app.MapGet("/users", async () =>
{
    await using var driver = GetDriver();
    var users = await driver.ExecutableQuery("MATCH (u:User) RETURN u.name AS Name").ExecuteAsync();
    return users.Result.Select(x => x["Name"].As<string>()).ToArray();
});

if (Environment.GetEnvironmentVariable("ADMIN_KEY") is not null)
{
    app.MapPost("/admin/seed", async ([FromHeader(Name = "X-AdminKey")] string? adminKey) =>
    {
        if (Environment.GetEnvironmentVariable("ADMIN_KEY") != adminKey)
        {
            return Results.NotFound();
        }
        
        await using var driver = GetDriver();
        await driver.ExecutableQuery("MATCH (n) DETACH DELETE n").ExecuteAsync();
        await driver.ExecutableQuery("CREATE (:User {name:'Chris'})").ExecuteAsync();
        await driver.ExecutableQuery("CREATE (:User {name:'Erica'})").ExecuteAsync();
        await driver.ExecutableQuery("CREATE (:User {name:'John'})").ExecuteAsync();
        await driver.ExecutableQuery("CREATE (:User {name:'Lisa'})").ExecuteAsync();

        return Results.Ok();
    });
}

app.MapGet("/chuck-quote", async (IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient();
    var quote = await client.GetFromJsonAsync<ChuckQuoteData>("https://chuckapi/jokes/random");
    return quote.Value;
});

app.MapDefaultEndpoints();

app.Run();

public record ChuckQuoteData(string Id, string IconUrl, string Url, string Value);
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}