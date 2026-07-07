using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using NBomber.Contracts;
using NBomber.CSharp;
using SagraFacile.Contracts.Common;
using SagraFacile.Contracts.Reservations;

namespace SagraFacile.Tests.Smoke.Scenarios;

public static class ReservationLifecycleScenario
{
    public static ScenarioProps Create(TestConfig config, HttpClient httpClient)
    {
        var duration = TimeSpan.FromSeconds(config.DurationSeconds);
        var warmup = TimeSpan.FromSeconds(30);

        return Scenario
            .Create("reservation_lifecycle", async ctx =>
            {
                var createStep = await Step.Run("create", ctx, async () =>
                {
                    using var response = await httpClient.PostAsJsonAsync(
                        "/api/reservations",
                        new CreateReservationRequest(
                            config.EventId,
                            new Bogus.Faker(locale: "it").Person.FirstName,
                            Random.Shared.Next(1, 16),
                            $"{ctx.ScenarioInfo.InstanceNumber}-{Guid.NewGuid():N}",
                            true),
                        cancellationToken: ctx.ScenarioCancellationToken);

                    if (!response.IsSuccessStatusCode)
                        return Response.Fail();

                    var createResult = await response.Content.ReadFromJsonAsync<CommandResult<CreateReservationResult>>(cancellationToken: ctx.ScenarioCancellationToken);
                    if (createResult is null || !createResult.Success || createResult.Data is null || createResult.Data.Id <= 0)
                        return Response.Fail(message: "Create response does not contain a valid reservation id");

                    ctx.Data["reservationId"] = createResult.Data.Id;
                    return Response.Ok(sizeBytes: (int)(response.Content.Headers.ContentLength ?? 0));
                });

                if (createStep.IsError)
                    return Response.Fail();

                await Task.Delay(Random.Shared.Next(2000, 5001), ctx.ScenarioCancellationToken);

                var reservationId = (int)ctx.Data["reservationId"];
                var callStep = await Step.Run("call", ctx, async () =>
                {
                    using var response = await httpClient.PostAsJsonAsync(
                        $"/api/reservations/{reservationId}/call",
                        new
                        {
                            calledBy = "SmokeTest"
                        },
                        cancellationToken: ctx.ScenarioCancellationToken);

                    return response.IsSuccessStatusCode
                        ? Response.Ok()
                        : Response.Fail();
                });

                if (callStep.IsError)
                    return Response.Fail();

                await Task.Delay(Random.Shared.Next(2000, 5001), ctx.ScenarioCancellationToken);

                var isVoid = Random.Shared.Next(100) == 0;
                var finalStep = await Step.Run(isVoid ? "void" : "seat", ctx, async () =>
                {
                    using var response = isVoid
                        ? await httpClient.DeleteAsync($"/api/reservations/{reservationId}", ctx.ScenarioCancellationToken)
                        : await httpClient.PostAsync($"/api/reservations/{reservationId}/seat", content: null, cancellationToken: ctx.ScenarioCancellationToken);

                    return response.IsSuccessStatusCode
                        ? Response.Ok()
                        : Response.Fail();
                });

                if (finalStep.IsError)
                {
                    ctx.Logger.Warning("Final step failed with message: {Message}",finalStep.Message);
                }

                return finalStep.IsError ? Response.Fail() : Response.Ok();
            })
            .WithLoadSimulations(
                Simulation.RampingInject(
                    rate: config.CreateRatePerMinute,
                    interval: TimeSpan.FromSeconds(15),
                    during: warmup
                ),
                Simulation.Inject(
                    rate: config.CreateRatePerMinute,
                    interval: TimeSpan.FromSeconds(30),
                    during: duration > warmup ? duration - warmup : TimeSpan.Zero)
                );
    }
}
