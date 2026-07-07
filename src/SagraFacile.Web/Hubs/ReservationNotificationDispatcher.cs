using Microsoft.AspNetCore.SignalR;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Contracts.Reservations;

namespace SagraFacile.Web.Hubs;

/// <summary>
/// Singleton background service that drains <see cref="ReservationNotificationChannel"/>
/// and forwards each message to SignalR clients.
/// The hub is in-process, so SignalR is always available; errors are logged and the loop continues.
/// </summary>
public sealed class ReservationNotificationDispatcher(
    ReservationNotificationChannel channel,
    IHubContext<ReservationHub, IReservationHubClient> hubContext,
    ILogger<ReservationNotificationDispatcher> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                switch (message)
                {
                    case StatusChangedMessage m:
                        await hubContext.Clients.All
                            .ReservationStatusChanged(m.Notification)
                            .WaitAsync(stoppingToken);
                        break;

                    case CountersUpdatedMessage m:
                        await hubContext.Clients.All
                            .CountersUpdated(m.Notification.Counters)
                            .WaitAsync(stoppingToken);
                        break;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unexpected error dispatching SignalR notification.");
            }
        }
    }
}