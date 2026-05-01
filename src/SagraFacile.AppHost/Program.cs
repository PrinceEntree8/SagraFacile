var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL resource named "DefaultConnection" so Aspire injects the connection string
// as ConnectionStrings__DefaultConnection, matching the key used in appsettings.json.
var postgres = builder.AddPostgres("DefaultConnection")
    .WithDataVolume("sagrafacile-data")
    .WithPgAdmin();

builder.AddProject<Projects.SagraFacile_Web>("web")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithEnvironment("AllowHttp", "true");

builder.Build().Run();
