var builder = DistributedApplication.CreateBuilder(args);

builder.Configuration.AddSharedResourcesConfiguration();

builder.AddNotificationService();

var apiService = builder.AddSampleApiservice()
    .ConfigureContainer(b => b.WithHttpEndpoint(targetPort: 8080))
    .WithHttpHealthCheck("/health");

builder.AddSampleWeb()
    .ConfigureContainer(b => b.WithHttpEndpoint(targetPort: 8080))
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService.GetEndpoint("http"))
    .WaitFor(apiService);

builder.Build().Run();
