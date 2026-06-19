var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL resource named "DefaultConnection" so Aspire injects the connection string
// as ConnectionStrings__DefaultConnection, matching the key used in appsettings.json.
var postgres = builder.AddPostgres("DefaultConnection")
    .WithDataVolume("sagrafacile-data")
    .WithUserName(builder.AddParameter("postgres-user", "sagrafacile"))
    .WithPassword(builder.AddParameter("postgres-password", "sagrafacile"))
    .WithPgAdmin();

var api = builder.AddProject<Projects.SagraFacile_Web>("web")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithExternalHttpEndpoints()
    .WithEnvironment("AllowHttp", "true");

var webclient = builder.AddProject<Projects.SagraFacile_WebClient>("webclient")
    .WithReference(api)
    .WaitFor(api);

var gateway = builder.AddYarp("gateway")
    .WithConfiguration(yarp =>
    {
        yarp.AddRoute(webclient);
        yarp.AddRoute("/api/{**catch-all}", api);
        yarp.AddRoute("/hubs/{**catch-all}", api);
    });

builder.Build().Run();
