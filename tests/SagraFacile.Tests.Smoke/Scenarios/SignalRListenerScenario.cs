using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using NBomber.Contracts;
using NBomber.CSharp;

namespace SagraFacile.Tests.Smoke.Scenarios;

public static class SignalRListenerScenario
{
    private static long _notificationsReceived;
    private static readonly ConcurrentDictionary<int, HubConnection> Connections = new();

    public static long NotificationsReceived => Interlocked.Read(ref _notificationsReceived);

    public static ScenarioProps Create(TestConfig config, string token)
    {
        var duration = TimeSpan.FromSeconds(config.DurationSeconds);

        return Scenario
            .Create("signalr_listeners", async ctx =>
            {
                var scenarioInfoThreadId = ctx.ScenarioInfo.InstanceNumber;
                if (!Connections.ContainsKey(scenarioInfoThreadId))
                {
                    var connection = new HubConnectionBuilder()
                        .WithUrl($"{config.BaseUrl}/hubs/reservations", options =>
                        {
                            options.AccessTokenProvider = () => Task.FromResult(token)!;
                        })
                        .WithAutomaticReconnect()
                        .Build();

                    connection.On<object>("ReservationStatusChanged", _ =>
                    {
                        Interlocked.Increment(ref _notificationsReceived);
                    });

                    await connection.StartAsync(ctx.ScenarioCancellationToken);
                    await connection.InvokeAsync("JoinReservationGroup", config.SignalRGroup, ctx.ScenarioCancellationToken);

                    Connections.TryAdd(scenarioInfoThreadId, connection);
                }

                await Task.Delay(ctx.Random.Next(60_000, 300_000), ctx.ScenarioCancellationToken);
                return Response.Ok();
            })
            .WithInit(_ =>
            {
                Interlocked.Exchange(ref _notificationsReceived, 0);
                Connections.Clear();
                return Task.CompletedTask;
            })
            .WithClean(async _ =>
            {
                foreach (var connection in Connections.Values)
                    await connection.DisposeAsync();

                Connections.Clear();
            })
            .WithLoadSimulations(
                Simulation.RampingConstant(200, TimeSpan.FromSeconds(30)),
                Simulation.KeepConstant(copies: 200, during: duration - TimeSpan.FromSeconds(30))
            );
    }
}
