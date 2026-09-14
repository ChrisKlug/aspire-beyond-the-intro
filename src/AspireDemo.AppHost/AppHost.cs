var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireDemo_ApiService>("apiservice")
        .WithHttpHealthCheck("/health");

builder.AddProject<Projects.AspireDemo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
