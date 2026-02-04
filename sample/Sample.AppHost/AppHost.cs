var builder = DistributedApplication.CreateBuilder(args);

builder.Configuration.AddSharedResourcesConfiguration();

builder.AddNotificationService();

var apiService = builder.AddProject<Projects.Sample_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Sample_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
