using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using SagraFacile.Tests.Smoke.Scenarios;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables(prefix: "SMOKETEST__")
    .Build();

var config = configuration.GetSection("SmokeTest").Get<TestConfig>()
             ?? throw new InvalidOperationException("Missing 'SmokeTest' configuration section.");

var token = await AuthService.LoginAsync(config);

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(config.BaseUrl)
};

httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

NBomberRunner
    .RegisterScenarios(
        SignalRListenerScenario.Create(config, token),
        ReservationLifecycleScenario.Create(config, httpClient))
    .WithTestName("SagraFacile Smoke Test")
    .WithReportFolder("reports")
    .WithReportFormats(ReportFormat.Csv, ReportFormat.Md)
    .Run();

Console.WriteLine($"Total SignalR notifications received: {SignalRListenerScenario.NotificationsReceived}");
