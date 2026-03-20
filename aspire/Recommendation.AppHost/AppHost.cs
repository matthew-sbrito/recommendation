IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresServerResource> postgres = builder
    .AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "pg17")
    .WithHostPort(5432)
    .WithDataVolume()
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<PostgresDatabaseResource> recommendationDatabase = postgres
    .AddDatabase("recommendation-db");

builder.AddProject<Projects.Web_Api>("web-api")
    .WithReference(recommendationDatabase)
    .WaitFor(recommendationDatabase);

await builder.Build().RunAsync();
