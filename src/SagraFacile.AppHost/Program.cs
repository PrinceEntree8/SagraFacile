var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL resource named "DefaultConnection" so Aspire injects the connection string
// as ConnectionStrings__DefaultConnection, matching the key used in appsettings.json.
var postgres = builder.AddPostgres("db")
    .WithDataVolume("sagrafacile-data")
    .WithUserName(builder.AddParameter("postgres-user", "sagrafacile"))
    .WithPassword(builder.AddParameter("postgres-password", "sagrafacile"))
    .WithHostPort(5432);

var api = builder.AddProject<Projects.SagraFacile_Web>("web")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithExternalHttpEndpoints()
    .WithEnvironment("AllowHttp", "true");

var gateway = builder.AddYarp("gateway")
    .WithHostPort(5100)
    .WithConfiguration(yarp =>
    {
        yarp.AddRoute("/api/{**catch-all}", api);
        yarp.AddRoute("/hubs/{**catch-all}", api);
        yarp.AddRoute(api);
    });

builder.Build().Run();
