var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddDotnetProject("apiservice", "../AspireDemo.ApiService")
        .WithHttpHealthCheck("/health");

builder.AddDotnetProject("webfrontend", "../AspireDemo.Web")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
