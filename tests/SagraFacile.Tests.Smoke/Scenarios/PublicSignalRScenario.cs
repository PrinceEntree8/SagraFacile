using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using NBomber.Contracts;
using NBomber.CSharp;

namespace SagraFacile.Tests.Smoke.Scenarios;

/// <summary>
/// Anonymous public realtime load: connects to /hubs/reservations WITHOUT an
/// access token (JoinReservationGroup/LeaveReservationGroup are [AllowAnonymous]
/// on ReservationHub and the hub endpoint itself has no authorization requirement),
/// joins the configured group and counts broadcast notifications.
/// </summary>
public static class PublicSignalRScenario
{
    private static long _notificationsReceived;
    private static readonly ConcurrentDictionary<int, HubConnection> Connections = new();

    public static long NotificationsReceived => Interlocked.Read(ref _notificationsReceived);

    public static ScenarioProps Create(TestConfig config)
    {
        var copies = config.PublicSignalRCopies;

        return Scenario
            .Create("public_signalr", async ctx =>
            {
                var instanceNumber = ctx.ScenarioInfo.InstanceNumber;
                if (!Connections.ContainsKey(instanceNumber))
                {
                    // Intentionally no AccessTokenProvider: anonymous connection.
                    var connection = new HubConnectionBuilder()
                        .WithUrl($"{config.BaseUrl}/hubs/reservations")
                        .WithAutomaticReconnect()
                        .Build();

                    connection.On<object>("ReservationStatusChanged", _ =>
                    {
                        Interlocked.Increment(ref _notificationsReceived);
                    });

                    connection.On<object>("CountersUpdated", _ =>
                    {
                        Interlocked.Increment(ref _notificationsReceived);
                    });

                    connection.On<int>("AvailableSeatsUpdated", _ =>
                    {
                        Interlocked.Increment(ref _notificationsReceived);
                    });

                    await connection.StartAsync(ctx.ScenarioCancellationToken);
                    await connection.InvokeAsync("JoinReservationGroup", config.SignalRGroup, ctx.ScenarioCancellationToken);

                    Connections.TryAdd(instanceNumber, connection);
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
                Simulation.RampingConstant(copies, TimeSpan.FromMinutes(2)),
                Simulation.KeepConstant(copies: copies, during: TimeSpan.FromMinutes(5))
            );
    }
}
