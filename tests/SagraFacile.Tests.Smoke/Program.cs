using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using SagraFacile.Tests.Smoke.Scenarios;

var builder = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

// Perf profile: set SMOKETEST_PROFILE=perf to layer appsettings.perf.json overrides.
if (string.Equals(Environment.GetEnvironmentVariable("SMOKETEST_PROFILE"), "perf", StringComparison.OrdinalIgnoreCase))
    builder.AddJsonFile("appsettings.perf.json", optional: false, reloadOnChange: false);

var configuration = builder
    .AddEnvironmentVariables(prefix: "SMOKETEST__")
    .Build();

var config = configuration.GetSection("SmokeTest").Get<TestConfig>()
             ?? throw new InvalidOperationException("Missing 'SmokeTest' configuration section.");

// Anonymous client for public pages: never gets an Authorization header.
using var anonymousClient = new HttpClient
{
    BaseAddress = new Uri(config.BaseUrl)
};

using var authenticatedClient = new HttpClient
{
    BaseAddress = new Uri(config.BaseUrl)
};

var scenarios = new List<ScenarioProps>();

if (config.EnablePublicPages)
    scenarios.Add(PublicPagesScenario.Create(config, anonymousClient));

if (config.EnablePublicSignalR)
    scenarios.Add(PublicSignalRScenario.Create(config));

if (config.EnableReservationLifecycle || config.EnableSignalRListeners)
{
    var token = await AuthService.LoginAsync(config);
    authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    if (config.EnableSignalRListeners)
        scenarios.Add(SignalRListenerScenario.Create(config, token));

    if (config.EnableReservationLifecycle)
        scenarios.Add(ReservationLifecycleScenario.Create(config, authenticatedClient));
}

if (scenarios.Count == 0)
    throw new InvalidOperationException("No scenarios enabled. Check the SmokeTest configuration flags.");

NBomberRunner
    .RegisterScenarios(scenarios.ToArray())
    .WithTestName("SagraFacile Smoke Test")
    .WithReportFolder("reports")
    .WithReportFormats(ReportFormat.Csv, ReportFormat.Md)
    .Run();

Console.WriteLine($"Total SignalR notifications received: {SignalRListenerScenario.NotificationsReceived}");
Console.WriteLine($"Total public SignalR notifications received: {PublicSignalRScenario.NotificationsReceived}");
