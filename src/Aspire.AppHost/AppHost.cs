IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresDatabaseResource> recommendationDatabase = builder
    .AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "pg17")
    .WithHostPort(5432)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("recommendation-db");

builder.AddProject<Projects.Web_Api>("web-api")
    .WithReference(recommendationDatabase)
    .WaitFor(recommendationDatabase);

await builder.Build().RunAsync();
